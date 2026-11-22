using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using CetakSertifikatApp.Models;
using CetakSertifikatApp.Services;

namespace CetakSertifikatApp.Views
{
    public partial class ExportDialog : Window
    {
        private readonly CertificateTemplate _template;
        private readonly List<RecipientRecord> _recipients;
        private CancellationTokenSource? _cts;
        private bool _isExporting = false;
        private string _lastExportPath = string.Empty;

        public ExportDialog(CertificateTemplate template, List<RecipientRecord> recipients)
        {
            InitializeComponent();
            _template = template;
            _recipients = recipients;

            TxtSubtitle.Text = $"Mengekspor {recipients.Count} sertifikat sesuai data penerima.";
            SetDefaultTargetPath();
        }

        private void SetDefaultTargetPath()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (RbMergedPdf.IsChecked == true)
            {
                TxtTargetPath.Text = Path.Combine(docs, "Semua_Sertifikat.pdf");
                LblTarget.Text = "Lokasi Simpan File PDF Gabungan:";
                PanelIndividualOptions.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtTargetPath.Text = Path.Combine(docs, "Sertifikat_Ekspor");
                LblTarget.Text = "Folder Tujuan:";
                PanelIndividualOptions.Visibility = Visibility.Visible;
            }
        }

        private void ExportMode_Changed(object sender, RoutedEventArgs e)
        {
            if (TxtTargetPath == null) return;
            SetDefaultTargetPath();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (RbMergedPdf.IsChecked == true)
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "File PDF (*.pdf)|*.pdf",
                    FileName = "Semua_Sertifikat.pdf",
                    Title = "Pilih Lokasi Simpan File PDF"
                };
                if (sfd.ShowDialog() == true)
                {
                    TxtTargetPath.Text = sfd.FileName;
                }
            }
            else
            {
                var fbd = new OpenFolderDialog
                {
                    Title = "Pilih Folder Tujuan Penyimpanan Sertifikat"
                };
                if (fbd.ShowDialog() == true)
                {
                    TxtTargetPath.Text = fbd.FolderName;
                }
            }
        }

        private async void BtnStartExport_Click(object sender, RoutedEventArgs e)
        {
            if (_isExporting) return;

            string target = TxtTargetPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                MessageBox.Show("Silakan tentukan lokasi penyimpanan terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExportFormat format = ExportFormat.MergedPdf;
            if (RbIndividualPdf.IsChecked == true) format = ExportFormat.IndividualPdf;
            else if (RbIndividualPng.IsChecked == true) format = ExportFormat.IndividualPng;

            _cts = new CancellationTokenSource();
            _isExporting = true;
            BtnStartExport.IsEnabled = false;
            BtnBrowse.IsEnabled = false;
            BtnCancel.Content = "Batal";
            PanelProgress.Visibility = Visibility.Visible;
            PbExport.Value = 0;
            TxtProgressPercent.Text = "0%";
            TxtProgressStatus.Text = "Memulai ekspor...";

            var progress = new Progress<ExportProgressReport>(report =>
            {
                PbExport.Value = report.Percentage;
                TxtProgressPercent.Text = $"{report.Percentage}%";
                TxtProgressStatus.Text = report.StatusMessage;
            });

            var result = await ExportService.ExportBatchAsync(
                _template,
                _recipients,
                format,
                target,
                TxtPattern.Text.Trim(),
                300,
                progress,
                _cts.Token);

            _isExporting = false;
            BtnStartExport.IsEnabled = true;
            BtnBrowse.IsEnabled = true;
            BtnCancel.Content = "Tutup";

            if (result.Success)
            {
                _lastExportPath = target;
                BtnOpenResult.Visibility = Visibility.Visible;
                MessageBox.Show(result.Message, "Ekspor Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (!_cts.IsCancellationRequested)
            {
                MessageBox.Show(result.Message, "Ekspor Gagal / Terkendala", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isExporting && _cts != null)
            {
                _cts.Cancel();
                TxtProgressStatus.Text = "Membatalkan proses...";
            }
            else
            {
                Close();
            }
        }

        private void BtnOpenResult_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastExportPath)) return;

            try
            {
                if (File.Exists(_lastExportPath))
                {
                    // Select file in explorer
                    Process.Start("explorer.exe", $"/select,\"{_lastExportPath}\"");
                }
                else if (Directory.Exists(_lastExportPath))
                {
                    // Open folder
                    Process.Start("explorer.exe", $"\"{_lastExportPath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tidak dapat membuka lokasi: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
