using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using CetakSertifikatApp.Models;

namespace CetakSertifikatApp.Services
{
    public class DataImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public List<RecipientRecord> Records { get; set; } = new();
    }

    public static class DataImportService
    {
        public static DataImportResult ImportFromExcel(string filePath)
        {
            var result = new DataImportResult();
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    result.Message = "File Excel tidak memiliki lembar kerja (worksheet).";
                    return result;
                }

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    result.Message = "Lembar kerja Excel kosong.";
                    return result;
                }

                int firstRow = range.FirstRow().RowNumber();
                int lastRow = range.LastRow().RowNumber();
                int firstCol = range.FirstColumn().ColumnNumber();
                int lastCol = range.LastColumn().ColumnNumber();

                // Read header row
                var colHeaders = new List<string>();
                for (int col = firstCol; col <= lastCol; col++)
                {
                    string header = worksheet.Cell(firstRow, col).GetString().Trim();
                    if (string.IsNullOrEmpty(header))
                    {
                        header = $"Kolom_{col}";
                    }
                    // Clean header: remove brackets if user wrote {nama} in Excel header
                    header = header.Trim('{', '}').Trim();
                    colHeaders.Add(header);
                }

                result.Columns = colHeaders;

                // Read data rows
                int recordIndex = 1;
                for (int row = firstRow + 1; row <= lastRow; row++)
                {
                    var record = new RecipientRecord { RowIndex = recordIndex };
                    bool hasData = false;

                    for (int c = 0; c < colHeaders.Count; c++)
                    {
                        int colNum = firstCol + c;
                        string cellVal = worksheet.Cell(row, colNum).GetString().Trim();
                        if (!string.IsNullOrEmpty(cellVal))
                        {
                            hasData = true;
                        }
                        record.Data[colHeaders[c]] = cellVal;
                    }

                    if (hasData)
                    {
                        result.Records.Add(record);
                        recordIndex++;
                    }
                }

                result.Success = true;
                result.Message = $"Berhasil memuat {result.Records.Count} baris data dari Excel.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Gagal membaca file Excel: {ex.Message}";
            }

            return result;
        }

        public static DataImportResult ImportFromCsv(string filePath)
        {
            var result = new DataImportResult();
            try
            {
                // Detect delimiter
                string firstLine = File.ReadLines(filePath).FirstOrDefault() ?? string.Empty;
                char delimiter = ',';
                if (firstLine.Count(c => c == ';') > firstLine.Count(c => c == ','))
                {
                    delimiter = ';';
                }
                else if (firstLine.Count(c => c == '\t') > firstLine.Count(c => c == ','))
                {
                    delimiter = '\t';
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = delimiter.ToString(),
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    HeaderValidated = null
                };

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                if (!csv.Read() || !csv.ReadHeader())
                {
                    result.Success = false;
                    result.Message = "File CSV kosong atau tidak memiliki baris header.";
                    return result;
                }

                string[] headers = csv.HeaderRecord ?? Array.Empty<string>();
                var colHeaders = headers.Select(h => (h ?? "").Trim().Trim('{', '}').Trim()).ToList();
                if (colHeaders.Count == 0 || colHeaders.All(string.IsNullOrWhiteSpace))
                {
                    result.Success = false;
                    result.Message = "File CSV tidak memiliki nama kolom pada baris header.";
                    return result;
                }
                result.Columns = colHeaders;

                int recordIndex = 1;
                while (csv.Read())
                {
                    var record = new RecipientRecord { RowIndex = recordIndex };
                    bool hasData = false;

                    for (int i = 0; i < colHeaders.Count; i++)
                    {
                        string val = csv.GetField(i)?.Trim() ?? string.Empty;
                        if (!string.IsNullOrEmpty(val))
                        {
                            hasData = true;
                        }
                        record.Data[colHeaders[i]] = val;
                    }

                    if (hasData)
                    {
                        result.Records.Add(record);
                        recordIndex++;
                    }
                }

                result.Success = true;
                result.Message = $"Berhasil memuat {result.Records.Count} baris data dari CSV.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Gagal membaca file CSV: {ex.Message}";
            }

            return result;
        }

        public static DataImportResult ImportFromText(string rawText)
        {
            var result = new DataImportResult();
            if (string.IsNullOrWhiteSpace(rawText))
            {
                result.Message = "Teks kosong.";
                return result;
            }

            try
            {
                string firstLine = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                char delimiter = '\t';
                if (firstLine.Contains('\t')) delimiter = '\t';
                else if (firstLine.Count(c => c == ';') > firstLine.Count(c => c == ',')) delimiter = ';';
                else delimiter = ',';

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = delimiter.ToString(),
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    HeaderValidated = null
                };

                using var reader = new StringReader(rawText);
                using var csv = new CsvReader(reader, config);

                if (!csv.Read() || !csv.ReadHeader())
                {
                    result.Message = "Tidak dapat membaca header baris pertama.";
                    return result;
                }

                string[] headers = csv.HeaderRecord ?? Array.Empty<string>();
                var colHeaders = headers.Select(h => (h ?? "").Trim().Trim('{', '}').Trim()).ToList();
                result.Columns = colHeaders;

                int recordIndex = 1;
                while (csv.Read())
                {
                    var record = new RecipientRecord { RowIndex = recordIndex };
                    bool hasData = false;

                    for (int i = 0; i < colHeaders.Count; i++)
                    {
                        string val = csv.GetField(i)?.Trim() ?? string.Empty;
                        if (!string.IsNullOrEmpty(val)) hasData = true;
                        record.Data[colHeaders[i]] = val;
                    }

                    if (hasData)
                    {
                        result.Records.Add(record);
                        recordIndex++;
                    }
                }

                result.Success = true;
                result.Message = $"Berhasil memuat {result.Records.Count} baris data.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Gagal memproses data teks: {ex.Message}";
            }

            return result;
        }

        public static List<RecipientRecord> GetSampleRecipients()
        {
            return new List<RecipientRecord>
            {
                new RecipientRecord
                {
                    RowIndex = 1,
                    Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "nama", "Budi Santoso, S.Kom." },
                        { "nomor", "001/SERTIF/KOM/2026" },
                        { "peran", "Peserta" },
                        { "kegiatan", "Workshop Pengembangan Aplikasi Desktop C#" },
                        { "instansi", "PT Teknologi Maju Bangsa" },
                        { "tanggal", "11 November 2026" }
                    }
                },
                new RecipientRecord
                {
                    RowIndex = 2,
                    Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Long academic title test as requested in prompt!
                        { "nama", "Dr. Ir. H. Muhammad Al-Fatih, M.Kom." },
                        { "nomor", "002/SERTIF/KOM/2026" },
                        { "peran", "Pemateri Utama" },
                        { "kegiatan", "Workshop Pengembangan Aplikasi Desktop C#" },
                        { "instansi", "Fakultas Ilmu Komputer, Universitas Indonesia" },
                        { "tanggal", "11 November 2026" }
                    }
                },
                new RecipientRecord
                {
                    RowIndex = 3,
                    Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "nama", "Siti Nurhaliza Rahmawati, S.Pd., M.Pd." },
                        { "nomor", "003/SERTIF/KOM/2026" },
                        { "peran", "Moderator" },
                        { "kegiatan", "Workshop Pengembangan Aplikasi Desktop C#" },
                        { "instansi", "Dinas Pendidikan Provinsi DKI Jakarta" },
                        { "tanggal", "11 November 2026" }
                    }
                }
            };
        }

        public static void GenerateExcelTemplate(string filePath, IEnumerable<string>? columnNames = null)
        {
            var cols = columnNames?.ToList() ?? new List<string>();
            if (cols.Count == 0)
            {
                cols = new List<string> { "nama", "nomor", "peran", "kegiatan", "instansi", "tanggal" };
            }

            using var workbook = new XLWorkbook();

            // Sheet 1: Data Penerima
            var ws = workbook.Worksheets.Add("Data Penerima");

            // Header styling
            for (int i = 0; i < cols.Count; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = cols[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.FontSize = 11;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(15, 32, 67); // Navy Blue
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(203, 213, 225);
            }
            ws.Row(1).Height = 28;

            // Sample pre-filled rows
            var sampleRows = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "nama", "Budi Santoso, S.Kom." },
                    { "nomor", "001/SERTIF/XI/2026" },
                    { "peran", "Peserta" },
                    { "kegiatan", "Workshop Desain & Cetak Sertifikat Digital" },
                    { "instansi", "PT Inovasi Digital Nusantara" },
                    { "tanggal", "12 November 2026" }
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "nama", "Dr. Ir. H. Muhammad Al-Fatih, M.Kom." },
                    { "nomor", "002/SERTIF/XI/2026" },
                    { "peran", "Narasumber Utama" },
                    { "kegiatan", "Workshop Desain & Cetak Sertifikat Digital" },
                    { "instansi", "Fakultas Ilmu Komputer, Universitas Indonesia" },
                    { "tanggal", "12 November 2026" }
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "nama", "Siti Nurhaliza Rahmawati, S.Pd., M.Pd." },
                    { "nomor", "003/SERTIF/XI/2026" },
                    { "peran", "Moderator" },
                    { "kegiatan", "Workshop Desain & Cetak Sertifikat Digital" },
                    { "instansi", "Dinas Pendidikan Provinsi DKI Jakarta" },
                    { "tanggal", "12 November 2026" }
                }
            };

            for (int r = 0; r < sampleRows.Count; r++)
            {
                int rowNum = r + 2;
                var data = sampleRows[r];
                for (int c = 0; c < cols.Count; c++)
                {
                    string colKey = cols[c];
                    var cell = ws.Cell(rowNum, c + 1);
                    if (data.TryGetValue(colKey, out var val))
                    {
                        cell.Value = val;
                    }
                    else
                    {
                        cell.Value = $"Contoh {colKey} {r + 1}";
                    }
                    cell.Style.Font.FontSize = 10;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(226, 232, 240);
                }
                ws.Row(rowNum).Height = 22;
            }

            // Empty bordered template rows for user convenience
            for (int r = 5; r <= 20; r++)
            {
                for (int c = 0; c < cols.Count; c++)
                {
                    var cell = ws.Cell(r, c + 1);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(241, 245, 249);
                }
                ws.Row(r).Height = 20;
            }

            ws.Columns().AdjustToContents(15.0, 50.0);

            // Sheet 2: Panduan Pengisian
            var wsGuide = workbook.Worksheets.Add("Panduan Pengisian");
            wsGuide.Cell(1, 1).Value = "PANDUAN PENGISIAN & PENGUMPULAN DATA SERTIFIKAT";
            wsGuide.Cell(1, 1).Style.Font.Bold = true;
            wsGuide.Cell(1, 1).Style.Font.FontSize = 14;
            wsGuide.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(15, 32, 67);

            var guideLines = new[]
            {
                "1. Baris 1 pada lembar 'Data Penerima' adalah nama kolom variabel (misal: nama, nomor, peran).",
                "2. Jangan mengubah nama kolom pada baris 1 agar aplikasi dapat mencocokkan teks sertifikat secara otomatis.",
                "3. Anda dapat menghapus data contoh pada baris 2-4 dan menggantinya dengan data peserta Anda.",
                "4. Gelar akademik, tanda baca, dan huruf khusus (koma, titik, kutip) dapat ditulis langsung (contoh: Dr. Ir. H. Muhammad Al-Fatih, M.Kom.).",
                "5. Jika mengumpulkan data peserta via Google Form:",
                "   - Ekspor respon Google Form ke Google Sheets.",
                "   - Salin kolom Nama dan data terkait, lalu tempelkan ke file Excel ini.",
                "   - Atau langsung klik tombol 'Tempel dari Clipboard (Ctrl+V)' pada aplikasi tanpa perlu menyimpan Excel terlebih dahulu!",
                "6. Setelah file diisi, simpan file ini dan buka aplikasi -> 'Kelola Data Penerima' -> 'Impor Excel (.xlsx)'."
            };

            for (int i = 0; i < guideLines.Length; i++)
            {
                var cell = wsGuide.Cell(i + 3, 1);
                cell.Value = guideLines[i];
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.FromArgb(51, 65, 85);
            }
            wsGuide.Column(1).AdjustToContents();

            workbook.SaveAs(filePath);
        }

        public static void GenerateCsvTemplate(string filePath, IEnumerable<string>? columnNames = null)
        {
            var cols = columnNames?.ToList() ?? new List<string>();
            if (cols.Count == 0)
            {
                cols = new List<string> { "nama", "nomor", "peran", "kegiatan", "instansi", "tanggal" };
            }

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            foreach (var col in cols)
            {
                csv.WriteField(col);
            }
            csv.NextRecord();

            // 2 sample rows
            csv.WriteField("Budi Santoso, S.Kom.");
            csv.WriteField("001/SERTIF/XI/2026");
            csv.WriteField("Peserta");
            csv.WriteField("Workshop Desain & Cetak Sertifikat Digital");
            csv.WriteField("PT Inovasi Digital Nusantara");
            csv.WriteField("12 November 2026");
            csv.NextRecord();

            csv.WriteField("Dr. Ir. H. Muhammad Al-Fatih, M.Kom.");
            csv.WriteField("002/SERTIF/XI/2026");
            csv.WriteField("Narasumber Utama");
            csv.WriteField("Workshop Desain & Cetak Sertifikat Digital");
            csv.WriteField("Fakultas Ilmu Komputer, Universitas Indonesia");
            csv.WriteField("12 November 2026");
            csv.NextRecord();
        }

        public static List<RecipientRecord> GenerateMockRecipients(int count, IEnumerable<string>? columns = null)
        {
            var cols = columns?.ToList() ?? new List<string> { "nama", "nomor", "peran", "kegiatan", "instansi", "tanggal" };
            var names = new[]
            {
                "Budi Santoso, S.Kom.",
                "Dr. Ir. H. Muhammad Al-Fatih, M.Kom.",
                "Siti Nurhaliza Rahmawati, S.Pd., M.Pd.",
                "Ahmad Fauzi, S.T., M.T.",
                "Prof. Dr. Ir. Sri Mulyani, M.Sc.",
                "Dewi Sartika, S.E., Ak., CA",
                "dr. Rizky Pratama, Sp.A.",
                "Hendra Wijaya, S.H., M.H.",
                "Nabila Putri Utami, B.Eng.",
                "Bambang Suryo Pranoto, M.M.",
                "Andi Muhammad Fadil, S.Si.",
                "Kurniawati Indah Permata, M.Psi.",
                "Muhammad Ilham Ramadhan, S.Ked.",
                "Rina Agustina, S.Sos., M.I.Kom.",
                "Dimas Aryo Tejo, S.Kom."
            };

            var roles = new[] { "Peserta", "Peserta Terbaik", "Narasumber", "Moderator", "Panitia Pelaksana" };
            var instansis = new[]
            {
                "Universitas Indonesia",
                "Institut Teknologi Bandung",
                "Universitas Gadjah Mada",
                "PT Telkom Indonesia (Persero) Tbk",
                "Kementerian Komunikasi dan Informatika",
                "Dinas Pendidikan Provinsi DKI Jakarta",
                "PT Bank Mandiri (Persero) Tbk",
                "SMA Negeri 1 Jakarta",
                "Universitas Airlangga"
            };

            var list = new List<RecipientRecord>();
            for (int i = 0; i < count; i++)
            {
                var rec = new RecipientRecord { RowIndex = i + 1 };
                string name = names[i % names.Length];
                if (i >= names.Length)
                {
                    name = $"{name} ({i + 1})";
                }

                foreach (var col in cols)
                {
                    string key = col.ToLowerInvariant();
                    if (key == "nama") rec.Data[col] = name;
                    else if (key == "nomor") rec.Data[col] = $"{i + 1:D3}/SERTIF/XI/2026";
                    else if (key == "peran") rec.Data[col] = roles[i % roles.Length];
                    else if (key == "kegiatan") rec.Data[col] = "Pelatihan & Workshop Nasional 2026";
                    else if (key == "instansi") rec.Data[col] = instansis[i % instansis.Length];
                    else if (key == "tanggal") rec.Data[col] = "12 November 2026";
                    else rec.Data[col] = $"{col}_{i + 1}";
                }
                list.Add(rec);
            }
            return list;
        }
    }
}
