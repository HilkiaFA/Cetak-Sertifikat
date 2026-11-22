using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using CetakSertifikatApp.Models;
using CetakSertifikatApp.Services;
using CetakSertifikatApp.ViewModels;
using CetakSertifikatApp.Views;

namespace CetakSertifikatApp
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void BtnUploadBg_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "File Gambar Background (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Semua File (*.*)|*.*",
                Title = "Pilih Gambar Background Sertifikat"
            };

            if (dlg.ShowDialog() == true)
            {
                ViewModel.SetBackground(dlg.FileName);
                ViewModel.StatusMessage = $"Background berhasil dimuat: {Path.GetFileName(dlg.FileName)}";

                if (ViewModel.DpiInfo.IsLowResolution)
                {
                    MessageBox.Show(
                        ViewModel.DpiInfo.Message,
                        "Peringatan Resolusi Cetak",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private void BtnManageData_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new DataEditorDialog(ViewModel.Recipients, ViewModel.Template.ExtractVariables())
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                ViewModel.Recipients = dlg.ResultRecipients;
                ViewModel.StatusMessage = $"Data diperbarui: {ViewModel.TotalRecipients} penerima sertifikat.";
            }
        }

        private void BtnQuickDownloadExcel_Click(object sender, RoutedEventArgs e)
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
                    var vars = ViewModel.Template.ExtractVariables();
                    DataImportService.GenerateExcelTemplate(sfd.FileName, vars);

                    var res = MessageBox.Show(
                        $"File template Excel berhasil dibuat dengan kolom variabel:\n{string.Join(", ", vars)}\n\nApakah Anda ingin membuka file tersebut sekarang?",
                        "Template Excel Siap Diisi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (res == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

        private void BtnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Template Sertifikat (*.json)|*.json",
                FileName = "template_sertifikat.json",
                Title = "Simpan Template Sertifikat"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    // Update template fields from viewmodel
                    ViewModel.Template.Fields.Clear();
                    foreach (var f in ViewModel.Fields)
                    {
                        ViewModel.Template.Fields.Add(f.Model);
                    }

                    TemplateService.SaveTemplate(ViewModel.Template, dlg.FileName, true);
                    ViewModel.StatusMessage = $"Template berhasil disimpan ke: {Path.GetFileName(dlg.FileName)}";
                    MessageBox.Show("Template dan konfigurasi sertifikat berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal menyimpan template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnLoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Template Sertifikat (*.json)|*.json|Semua File (*.*)|*.*",
                Title = "Buka Template Sertifikat"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var tmpl = TemplateService.LoadTemplate(dlg.FileName);
                    ViewModel.LoadFieldsFromTemplate(tmpl);
                    ViewModel.StatusMessage = $"Template dimuat: {Path.GetFileName(dlg.FileName)}";
                    MessageBox.Show("Template sertifikat berhasil dimuat!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal memuat template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSampleTemplate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Apakah Anda ingin memuat ulang template contoh bawaan (A4 Lanskap + Background Sertifikat Elegan)?",
                "Konfirmasi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                string sampleBg = SampleCertificateGenerator.EnsureSampleBackgroundExists();
                var defaultTmpl = TemplateService.CreateDefaultTemplate(sampleBg);
                ViewModel.LoadFieldsFromTemplate(defaultTmpl);
                ViewModel.Recipients = DataImportService.GetSampleRecipients();
                ViewModel.StatusMessage = "Template contoh bawaan berhasil diterapkan.";
            }
        }

        private void BtnExportBatch_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.TotalRecipients == 0)
            {
                MessageBox.Show("Daftar penerima masih kosong. Silakan impor atau masukkan data terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Sync fields
            ViewModel.Template.Fields.Clear();
            foreach (var f in ViewModel.Fields)
            {
                ViewModel.Template.Fields.Add(f.Model);
            }

            var dlg = new ExportDialog(ViewModel.Template, ViewModel.Recipients)
            {
                Owner = this
            };
            dlg.ShowDialog();
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentRecipient == null)
            {
                MessageBox.Show("Tidak ada data sertifikat untuk dicetak.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                try
                {
                    // Render high resolution bitmap (300 DPI) for physical printing
                    BitmapSource printBmp = CertificateRenderer.RenderToWpfBitmap(ViewModel.Template, ViewModel.CurrentRecipient, 300);

                    var image = new Image
                    {
                        Source = printBmp,
                        Width = printDlg.PrintableAreaWidth,
                        Height = printDlg.PrintableAreaHeight,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };

                    printDlg.PrintVisual(image, $"Sertifikat - {ViewModel.CurrentRecipient.GetValue("nama", "Peserta")}");
                    ViewModel.StatusMessage = "Sertifikat berhasil dikirim ke printer.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal mencetak: {ex.Message}", "Error Cetak", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddField_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddNewField("{nama}");
        }

        private void BtnDuplicateField_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DuplicateField(ViewModel.SelectedField);
        }

        private void BtnDeleteField_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteField(ViewModel.SelectedField);
        }

        private void BtnInsertVariable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string varName && ViewModel.SelectedField != null)
            {
                string tag = varName; // e.g. "{nama}"
                int idx = TxtFieldText.SelectionStart;
                string curText = ViewModel.SelectedField.Text ?? string.Empty;

                if (idx >= 0 && idx <= curText.Length)
                {
                    ViewModel.SelectedField.Text = curText.Insert(idx, tag);
                    TxtFieldText.SelectionStart = idx + tag.Length;
                }
                else
                {
                    ViewModel.SelectedField.Text += " " + tag;
                }
                TxtFieldText.Focus();
            }
        }

        private void BtnPickColor_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedField == null) return;

            var dlg = new InputColorDialog(ViewModel.SelectedField.Color)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                ViewModel.SelectedField.Color = dlg.SelectedHexColor;
            }
        }

        private void Swatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex && ViewModel.SelectedField != null)
            {
                ViewModel.SelectedField.Color = hex;
            }
        }

        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedField != null) ViewModel.SelectedField.Align = "left";
        }

        private void AlignCenter_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedField != null) ViewModel.SelectedField.Align = "center";
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedField != null) ViewModel.SelectedField.Align = "right";
        }

        private void BtnCenterField_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CenterFieldHorizontally(ViewModel.SelectedField);
        }

        private void BtnCenterFieldV_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CenterFieldVertically(ViewModel.SelectedField);
        }

        private void BtnNudgeLeft_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NudgeField(ViewModel.SelectedField, -1.0, 0);
        }

        private void BtnNudgeRight_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NudgeField(ViewModel.SelectedField, 1.0, 0);
        }

        private void BtnNudgeUp_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NudgeField(ViewModel.SelectedField, 0, -1.0);
        }

        private void BtnNudgeDown_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NudgeField(ViewModel.SelectedField, 0, 1.0);
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Undo();
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Redo();
        }

        private void BtnCut_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CutSelectedField();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CopySelectedField();
        }

        private void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PasteField();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool isCtrl = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control;
            bool isShift = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift;

            // Jika kursor sedang berada di dalam TextBox input teks, biarkan TextBox memproses pengetikan standar (kecuali Ctrl+D)
            if (System.Windows.Input.Keyboard.FocusedElement is TextBox tb && !tb.IsReadOnly)
            {
                if (isCtrl && e.Key == System.Windows.Input.Key.D)
                {
                    ViewModel.DuplicateField(ViewModel.SelectedField);
                    e.Handled = true;
                }
                return;
            }

            // 1. Ctrl + Z (Undo) / Ctrl + Shift + Z (Redo)
            if (isCtrl && e.Key == System.Windows.Input.Key.Z)
            {
                if (isShift)
                {
                    ViewModel.Redo();
                }
                else
                {
                    ViewModel.Undo();
                }
                e.Handled = true;
                return;
            }

            // 2. Ctrl + Y (Redo)
            if (isCtrl && e.Key == System.Windows.Input.Key.Y)
            {
                ViewModel.Redo();
                e.Handled = true;
                return;
            }

            // 3. Ctrl + C (Copy)
            if (isCtrl && e.Key == System.Windows.Input.Key.C)
            {
                if (ViewModel.SelectedField != null)
                {
                    ViewModel.CopySelectedField();
                    e.Handled = true;
                }
                return;
            }

            // 4. Ctrl + X (Cut)
            if (isCtrl && e.Key == System.Windows.Input.Key.X)
            {
                if (ViewModel.SelectedField != null)
                {
                    ViewModel.CutSelectedField();
                    e.Handled = true;
                }
                return;
            }

            // 5. Ctrl + V (Paste)
            if (isCtrl && e.Key == System.Windows.Input.Key.V)
            {
                ViewModel.PasteField();
                e.Handled = true;
                return;
            }

            // 6. Ctrl + D (Duplicate)
            if (isCtrl && e.Key == System.Windows.Input.Key.D)
            {
                if (ViewModel.SelectedField != null)
                {
                    ViewModel.DuplicateField(ViewModel.SelectedField);
                    e.Handled = true;
                }
                return;
            }

            // 7. Delete key (Hapus item)
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                if (ViewModel.SelectedField != null)
                {
                    ViewModel.DeleteField(ViewModel.SelectedField);
                    e.Handled = true;
                }
                return;
            }

            // 8. Arrow Keys Nudging (Geser posisi panah)
            if (ViewModel.SelectedField == null) return;

            double step = 1.0; // Langkah normal: 1 mm
            if (isShift)
            {
                step = 5.0; // Tahan Shift: 5 mm (geser lebih cepat)
            }
            else if (isCtrl)
            {
                step = 0.2; // Tahan Ctrl: 0.2 mm (geser mikro presisi)
            }

            bool handled = false;
            switch (e.Key)
            {
                case System.Windows.Input.Key.Left:
                    ViewModel.NudgeField(ViewModel.SelectedField, -step, 0);
                    handled = true;
                    break;
                case System.Windows.Input.Key.Right:
                    ViewModel.NudgeField(ViewModel.SelectedField, step, 0);
                    handled = true;
                    break;
                case System.Windows.Input.Key.Up:
                    ViewModel.NudgeField(ViewModel.SelectedField, 0, -step);
                    handled = true;
                    break;
                case System.Windows.Input.Key.Down:
                    ViewModel.NudgeField(ViewModel.SelectedField, 0, step);
                    handled = true;
                    break;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }

        private void BtnFirstRec_Click(object sender, RoutedEventArgs e) => ViewModel.FirstRecipient();
        private void BtnPrevRec_Click(object sender, RoutedEventArgs e) => ViewModel.PrevRecipient();
        private void BtnNextRec_Click(object sender, RoutedEventArgs e) => ViewModel.NextRecipient();
        private void BtnLastRec_Click(object sender, RoutedEventArgs e) => ViewModel.LastRecipient();

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e) => ViewModel.Zoom += 0.15;
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => ViewModel.Zoom -= 0.15;
        private void BtnZoomFit_Click(object sender, RoutedEventArgs e) => ViewModel.Zoom = 0.85;
    }
}
