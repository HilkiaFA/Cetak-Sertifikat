using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CetakSertifikatApp.Models;
using CetakSertifikatApp.Services;

namespace CetakSertifikatApp.Views
{
    public partial class DataEditorDialog : Window
    {
        private DataTable _dataTable = new();
        private readonly List<string> _templateVariables = new();

        public List<RecipientRecord> ResultRecipients { get; private set; } = new();

        public DataEditorDialog(List<RecipientRecord> initialRecipients, IEnumerable<string>? templateVariables = null)
        {
            InitializeComponent();

            if (templateVariables != null)
            {
                _templateVariables = templateVariables.ToList();
            }

            PopulateTable(initialRecipients);
        }

        private void PopulateTable(List<RecipientRecord> recipients)
        {
            _dataTable = new DataTable();

            if (recipients == null || recipients.Count == 0)
            {
                recipients = DataImportService.GetSampleRecipients();
            }

            // Find all unique column keys
            var colNames = new List<string>();

            // Prioritize template variables if available
            foreach (var v in _templateVariables)
            {
                if (!colNames.Contains(v, StringComparer.OrdinalIgnoreCase))
                {
                    colNames.Add(v);
                }
            }

            foreach (var r in recipients)
            {
                foreach (var k in r.Data.Keys)
                {
                    if (!colNames.Contains(k, StringComparer.OrdinalIgnoreCase))
                    {
                        colNames.Add(k);
                    }
                }
            }

            if (colNames.Count == 0)
            {
                colNames.AddRange(new[] { "nama", "nomor", "peran", "kegiatan", "instansi", "tanggal" });
            }

            foreach (var col in colNames)
            {
                _dataTable.Columns.Add(col, typeof(string));
            }

            foreach (var r in recipients)
            {
                var row = _dataTable.NewRow();
                foreach (var col in colNames)
                {
                    row[col] = r.GetValue(col);
                }
                _dataTable.Rows.Add(row);
            }

            GridRecipients.ItemsSource = _dataTable.DefaultView;
            UpdateCountInfo();
        }

        private List<string> GetCurrentColumns()
        {
            var list = new List<string>();
            foreach (DataColumn col in _dataTable.Columns)
            {
                list.Add(col.ColumnName);
            }
            if (list.Count == 0 && _templateVariables.Count > 0)
            {
                list.AddRange(_templateVariables);
            }
            return list;
        }

        private void UpdateCountInfo()
        {
            TxtInfo.Text = $"Total: {_dataTable.Rows.Count} baris data, {_dataTable.Columns.Count} kolom variabel.";
        }

        private void GridRecipients_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Header = $"{{{e.PropertyName}}}";
        }

        private void BtnDownloadExcelTemplate_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "File Excel (*.xlsx)|*.xlsx",
                FileName = "Template_Data_Sertifikat.xlsx",
                Title = "Simpan File Template Excel"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var cols = GetCurrentColumns();
                    DataImportService.GenerateExcelTemplate(sfd.FileName, cols);

                    var res = MessageBox.Show(
                        $"File template Excel berhasil dibuat dengan kolom:\n{string.Join(", ", cols)}\n\nApakah Anda ingin membuka file tersebut sekarang?",
                        "Template Excel Berhasil Dibuat",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (res == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal membuat template Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDownloadCsvTemplate_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "File CSV (*.csv)|*.csv",
                FileName = "Template_Data_Sertifikat.csv",
                Title = "Simpan File Template CSV"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var cols = GetCurrentColumns();
                    DataImportService.GenerateCsvTemplate(sfd.FileName, cols);
                    MessageBox.Show("File template CSV berhasil dibuat!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal membuat template CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnShowGuide_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new GoogleFormGuideDialog { Owner = this };
            dlg.ShowDialog();
        }

        private void BtnAddColumn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddColumnDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                string colName = dlg.ColumnName;
                if (_dataTable.Columns.Contains(colName))
                {
                    MessageBox.Show($"Kolom '{colName}' sudah ada dalam tabel.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _dataTable.Columns.Add(colName, typeof(string));
                UpdateCountInfo();
                MessageBox.Show($"Kolom '{{{colName}}}' berhasil ditambahkan! Anda sekarang dapat mengisinya di tabel atau mengunduh template Excel baru.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnGenerateMock_Click(object sender, RoutedEventArgs e)
        {
            var cols = GetCurrentColumns();
            var mockRecipients = DataImportService.GenerateMockRecipients(25, cols);
            PopulateTable(mockRecipients);
            MessageBox.Show("Berhasil membuat 25 baris data peserta contoh realistis untuk simulasi sertifikat!", "Simulasi Data Dibuat", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Apakah Anda yakin ingin mengosongkan seluruh baris data?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _dataTable.Rows.Clear();
                UpdateCountInfo();
            }
        }

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "File Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Semua File (*.*)|*.*",
                Title = "Pilih File Excel Daftar Penerima"
            };

            if (dlg.ShowDialog() == true)
            {
                var res = DataImportService.ImportFromExcel(dlg.FileName);
                if (res.Success && res.Records.Count > 0)
                {
                    PopulateTable(res.Records);
                    MessageBox.Show(res.Message, "Sukses Impor Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(res.Message, "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "File CSV (*.csv)|*.csv|Semua File (*.*)|*.*",
                Title = "Pilih File CSV Daftar Penerima"
            };

            if (dlg.ShowDialog() == true)
            {
                var res = DataImportService.ImportFromCsv(dlg.FileName);
                if (res.Success && res.Records.Count > 0)
                {
                    PopulateTable(res.Records);
                    MessageBox.Show(res.Message, "Sukses Impor CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(res.Message, "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnPasteClipboard_Click(object sender, RoutedEventArgs e)
        {
            string clip = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clip))
            {
                MessageBox.Show("Clipboard kosong. Salin tabel data dari Excel atau Google Sheets terlebih dahulu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = DataImportService.ImportFromText(clip);
            if (res.Success && res.Records.Count > 0)
            {
                PopulateTable(res.Records);
                MessageBox.Show(res.Message, "Sukses Tempel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            var row = _dataTable.NewRow();
            _dataTable.Rows.Add(row);
            UpdateCountInfo();
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (GridRecipients.SelectedItem is DataRowView drv)
            {
                drv.Row.Delete();
                UpdateCountInfo();
            }
            else
            {
                MessageBox.Show("Pilih baris yang ingin dihapus terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnLoadSample_Click(object sender, RoutedEventArgs e)
        {
            PopulateTable(DataImportService.GetSampleRecipients());
        }

        private void BtnSaveApply_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<RecipientRecord>();
            int idx = 1;

            foreach (DataRow row in _dataTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                var rec = new RecipientRecord { RowIndex = idx };
                bool hasData = false;

                foreach (DataColumn col in _dataTable.Columns)
                {
                    string val = row[col]?.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(val)) hasData = true;
                    rec.Data[col.ColumnName] = val;
                }

                if (hasData)
                {
                    list.Add(rec);
                    idx++;
                }
            }

            if (list.Count == 0)
            {
                MessageBox.Show("Daftar data masih kosong. Silakan tambahkan minimal 1 baris.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultRecipients = list;
            DialogResult = true;
            Close();
        }
    }
}
