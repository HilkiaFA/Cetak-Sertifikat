using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CetakSertifikatApp.Models;

namespace CetakSertifikatApp.Services
{
    public enum ExportFormat
    {
        MergedPdf,
        IndividualPdf,
        IndividualPng
    }

    public class ExportProgressReport
    {
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public int Percentage => TotalCount > 0 ? (int)((CurrentIndex / (double)TotalCount) * 100) : 0;
        public string CurrentName { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public int ExportedCount { get; set; }
        public int FailedCount { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    public static class ExportService
    {
        public static async Task<ExportResult> ExportBatchAsync(
            CertificateTemplate template,
            List<RecipientRecord> recipients,
            ExportFormat format,
            string targetPathOrFolder,
            string filenamePattern = "{nama}_{nomor}",
            int pngDpi = 300,
            IProgress<ExportProgressReport>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ExportResult { OutputPath = targetPathOrFolder };

            if (recipients == null || recipients.Count == 0)
            {
                result.Message = "Tidak ada daftar nama/data penerima untuk diekspor.";
                return result;
            }

            try
            {
                await Task.Run(() =>
                {
                    int total = recipients.Count;

                    if (format == ExportFormat.MergedPdf)
                    {
                        // Single merged PDF
                        progress?.Report(new ExportProgressReport
                        {
                            CurrentIndex = 0,
                            TotalCount = total,
                            StatusMessage = "Mempersiapkan dokumen PDF gabungan..."
                        });

                        var subProgress = new Progress<int>(pct =>
                        {
                            int cur = (int)(pct * total / 100.0);
                            progress?.Report(new ExportProgressReport
                            {
                                CurrentIndex = cur,
                                TotalCount = total,
                                StatusMessage = $"Mengekspor halaman {cur} dari {total}..."
                            });
                        });

                        CertificateRenderer.ExportToPdf(targetPathOrFolder, template, recipients, subProgress, cancellationToken);

                        result.ExportedCount = total;
                        result.Success = true;
                        result.Message = $"Berhasil mengekspor {total} sertifikat ke dalam 1 file PDF:\n{targetPathOrFolder}";
                    }
                    else
                    {
                        // Individual PDF or PNG files
                        Directory.CreateDirectory(targetPathOrFolder);

                        using var sharedBg = CertificateRenderer.LoadBitmap(template.Background);

                        for (int i = 0; i < total; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var rec = recipients[i];
                            string recipientName = rec.GetValue("nama", $"Penerima_{i + 1}");

                            progress?.Report(new ExportProgressReport
                            {
                                CurrentIndex = i + 1,
                                TotalCount = total,
                                CurrentName = recipientName,
                                StatusMessage = $"Memproses ({i + 1}/{total}): {recipientName}"
                            });

                            try
                            {
                                string baseName = rec.ReplacePlaceholders(filenamePattern);
                                if (string.IsNullOrWhiteSpace(baseName))
                                {
                                    baseName = $"{i + 1:D3}_{recipientName}";
                                }

                                string safeName = SanitizeFilename(baseName);

                                if (format == ExportFormat.IndividualPdf)
                                {
                                    string outPdf = Path.Combine(targetPathOrFolder, safeName + ".pdf");
                                    CertificateRenderer.ExportSingleToPdf(outPdf, template, rec, sharedBg);
                                }
                                else
                                {
                                    string outPng = Path.Combine(targetPathOrFolder, safeName + ".png");
                                    CertificateRenderer.ExportSingleToPng(outPng, template, rec, pngDpi, sharedBg);
                                }

                                result.ExportedCount++;
                            }
                            catch (Exception ex)
                            {
                                result.FailedCount++;
                                result.Errors.Add($"Baris {i + 1} ({recipientName}): {ex.Message}");
                            }
                        }

                        result.Success = result.ExportedCount > 0;
                        result.Message = $"Selesai. Berhasil: {result.ExportedCount}, Gagal: {result.FailedCount}. Lokasi: {targetPathOrFolder}";
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = "Proses ekspor dibatalkan oleh pengguna.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Terjadi kesalahan saat ekspor: {ex.Message}";
            }

            return result;
        }

        public static string SanitizeFilename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "sertifikat";

            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            foreach (char c in "/\\:*?\"<>|")
            {
                invalid.Add(c);
            }

            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (char.IsControl(c) || invalid.Contains(c))
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(c);
                }
            }

            string clean = Regex.Replace(sb.ToString(), @"\s+", " ").Trim('.', ' ');
            if (clean.Length > 100)
            {
                clean = clean.Substring(0, 100).Trim('.', ' ');
            }

            return string.IsNullOrEmpty(clean) ? "sertifikat" : clean;
        }
    }
}
