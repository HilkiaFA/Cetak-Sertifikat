using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ClosedXML.Excel;
using SkiaSharp;
using CetakSertifikatApp.Models;
using CetakSertifikatApp.Services;
using CetakSertifikatApp.ViewModels;

namespace CetakSertifikatApp.Tests
{
    public class CertificateAppTests
    {
        [Fact]
        public void RecipientRecord_ShouldReplacePlaceholdersCaseInsensitively()
        {
            var record = new RecipientRecord();
            record.Data["nama"] = "Dr. Ir. H. Muhammad Al-Fatih, M.Kom.";
            record.Data["nomor"] = "001/SERTIF/2026";
            record.Data["peran"] = "Pemateri Utama";

            string template1 = "Diberikan kepada {nama} dengan No: {nomor} sebagai {peran}";
            string template2 = "Diberikan kepada {NAMA} dengan No: {NOMOR} sebagai {Peran}";

            string res1 = record.ReplacePlaceholders(template1);
            string res2 = record.ReplacePlaceholders(template2);

            Assert.Equal("Diberikan kepada Dr. Ir. H. Muhammad Al-Fatih, M.Kom. dengan No: 001/SERTIF/2026 sebagai Pemateri Utama", res1);
            Assert.Equal("Diberikan kepada Dr. Ir. H. Muhammad Al-Fatih, M.Kom. dengan No: 001/SERTIF/2026 sebagai Pemateri Utama", res2);
        }

        [Fact]
        public void DataImportService_ImportFromText_ShouldParseCorrectly()
        {
            string csvData = "nama,nomor,peran\n" +
                             "Budi Santoso,001,Peserta\n" +
                             "\"Dr. Ir. H. Muhammad Al-Fatih, M.Kom.\",002,Pemateri";

            var result = DataImportService.ImportFromText(csvData);

            Assert.True(result.Success);
            Assert.Equal(2, result.Records.Count);
            Assert.Equal("Budi Santoso", result.Records[0].GetValue("nama"));
            Assert.Equal("001", result.Records[0].GetValue("nomor"));
            Assert.Equal("Dr. Ir. H. Muhammad Al-Fatih, M.Kom.", result.Records[1].GetValue("nama"));
        }

        [Fact]
        public void DataImportService_ImportFromExcel_ShouldReadProperly()
        {
            string tempExcel = Path.Combine(Path.GetTempPath(), $"test_data_{Guid.NewGuid():N}.xlsx");

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Data");
                    ws.Cell(1, 1).Value = "nama";
                    ws.Cell(1, 2).Value = "nomor";
                    ws.Cell(1, 3).Value = "peran";

                    ws.Cell(2, 1).Value = "Dr. Ir. H. Muhammad Al-Fatih, M.Kom.";
                    ws.Cell(2, 2).Value = "001/KOM/2026";
                    ws.Cell(2, 3).Value = "Narasumber Utama";

                    ws.Cell(3, 1).Value = "Siti Rahmawati, S.Pd.";
                    ws.Cell(3, 2).Value = "002/KOM/2026";
                    ws.Cell(3, 3).Value = "Peserta";

                    wb.SaveAs(tempExcel);
                }

                var res = DataImportService.ImportFromExcel(tempExcel);

                Assert.True(res.Success);
                Assert.Equal(2, res.Records.Count);
                Assert.Equal("Dr. Ir. H. Muhammad Al-Fatih, M.Kom.", res.Records[0].GetValue("nama"));
                Assert.Equal("001/KOM/2026", res.Records[0].GetValue("nomor"));
                Assert.Equal("Narasumber Utama", res.Records[0].GetValue("peran"));
            }
            finally
            {
                if (File.Exists(tempExcel)) File.Delete(tempExcel);
            }
        }

        [Fact]
        public void TemplateService_SaveAndLoad_ShouldPreserveAllAttributes()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_template_{Guid.NewGuid():N}.json");

            try
            {
                var original = new CertificateTemplate
                {
                    Background = "sample_bg.png",
                    PageSize = "A4-landscape",
                    CustomWidthMm = 297.0,
                    CustomHeightMm = 210.0,
                    Fields = new List<TextFieldDefinition>
                    {
                        new TextFieldDefinition
                        {
                            Id = "nama",
                            Text = "{nama}",
                            FontFamily = "Arial",
                            FontSize = 36.0,
                            Color = "#1A1A1A",
                            Align = "center",
                            X = 148.5,
                            Y = 95.0,
                            BoxWidth = 200.0,
                            AutoShrink = true,
                            IsBold = true
                        }
                    }
                };

                TemplateService.SaveTemplate(original, tempFile, copyBackgroundToFolder: false);

                var loaded = TemplateService.LoadTemplate(tempFile);

                Assert.Equal(original.PageSize, loaded.PageSize);
                Assert.Equal(297.0, loaded.GetWidthMm());
                Assert.Equal(210.0, loaded.GetHeightMm());
                Assert.Single(loaded.Fields);

                var f = loaded.Fields[0];
                Assert.Equal("nama", f.Id);
                Assert.Equal("{nama}", f.Text);
                Assert.Equal(36.0, f.FontSize);
                Assert.Equal("#1A1A1A", f.Color);
                Assert.Equal("center", f.Align);
                Assert.Equal(148.5, f.X);
                Assert.Equal(95.0, f.Y);
                Assert.Equal(200.0, f.BoxWidth);
                Assert.True(f.AutoShrink);
                Assert.True(f.IsBold);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void ExportService_SanitizeFilename_ShouldRemoveInvalidChars()
        {
            string rawName = "Dr. Ir. H. Muhammad Al-Fatih, M.Kom./001:SERTIF*TEST?\"<>|";
            string sanitized = ExportService.SanitizeFilename(rawName);

            Assert.DoesNotContain("/", sanitized);
            Assert.DoesNotContain("\\", sanitized);
            Assert.DoesNotContain(":", sanitized);
            Assert.DoesNotContain("*", sanitized);
            Assert.DoesNotContain("?", sanitized);
            Assert.DoesNotContain("\"", sanitized);
            Assert.DoesNotContain("<", sanitized);
            Assert.DoesNotContain(">", sanitized);
            Assert.DoesNotContain("|", sanitized);
        }

        [Fact]
        public void CertificateRenderer_ExportToPdf_ShouldGenerateValidPdfFile()
        {
            string outPdf = Path.Combine(Path.GetTempPath(), $"test_cert_{Guid.NewGuid():N}.pdf");

            try
            {
                var template = TemplateService.CreateDefaultTemplate();
                var recipients = DataImportService.GetSampleRecipients();

                CertificateRenderer.ExportToPdf(outPdf, template, recipients);

                Assert.True(File.Exists(outPdf));
                var fileInfo = new FileInfo(outPdf);
                Assert.True(fileInfo.Length > 500, "PDF file must be non-empty and reasonably sized.");

                // Check PDF header
                byte[] header = new byte[5];
                using (var fs = File.OpenRead(outPdf))
                {
                    int read = fs.Read(header, 0, 5);
                    Assert.Equal(5, read);
                }
                string headerStr = Encoding.ASCII.GetString(header);
                Assert.Equal("%PDF-", headerStr);
            }
            finally
            {
                if (File.Exists(outPdf)) File.Delete(outPdf);
            }
        }

        [Fact]
        public void CertificateRenderer_ExportSingleToPng_ShouldGenerateValidPng()
        {
            string outPng = Path.Combine(Path.GetTempPath(), $"test_cert_{Guid.NewGuid():N}.png");

            try
            {
                var template = TemplateService.CreateDefaultTemplate();
                var recipient = DataImportService.GetSampleRecipients()[1]; // "Dr. Ir. H. Muhammad Al-Fatih, M.Kom."

                CertificateRenderer.ExportSingleToPng(outPng, template, recipient, dpi: 150);

                Assert.True(File.Exists(outPng));
                var fileInfo = new FileInfo(outPng);
                Assert.True(fileInfo.Length > 1000, "PNG file must be non-empty.");

                // Check PNG signature bytes: 0x89 0x50 0x4E 0x47
                byte[] sig = new byte[4];
                using (var fs = File.OpenRead(outPng))
                {
                    int read = fs.Read(sig, 0, 4);
                    Assert.Equal(4, read);
                }
                Assert.Equal(0x89, sig[0]);
                Assert.Equal(0x50, sig[1]);
                Assert.Equal(0x4E, sig[2]);
                Assert.Equal(0x47, sig[3]);
            }
            finally
            {
                if (File.Exists(outPng)) File.Delete(outPng);
            }
        }

        [Fact]
        public async Task ExportService_ExportBatch_MergedPdf_ShouldReportProgressAndComplete()
        {
            string outPdf = Path.Combine(Path.GetTempPath(), $"batch_cert_{Guid.NewGuid():N}.pdf");

            try
            {
                var template = TemplateService.CreateDefaultTemplate();
                var recipients = DataImportService.GetSampleRecipients();

                var progressReports = new List<ExportProgressReport>();
                var progress = new Progress<ExportProgressReport>(p => progressReports.Add(p));

                var result = await ExportService.ExportBatchAsync(
                    template,
                    recipients,
                    ExportFormat.MergedPdf,
                    outPdf,
                    progress: progress);

                Assert.True(result.Success);
                Assert.Equal(recipients.Count, result.ExportedCount);
                Assert.True(File.Exists(outPdf));
            }
            finally
            {
                if (File.Exists(outPdf)) File.Delete(outPdf);
            }
        }

        [Fact]
        public void CertificateRenderer_DpiCheck_ShouldWarnLowResolution()
        {
            string smallImgPath = Path.Combine(Path.GetTempPath(), $"small_bg_{Guid.NewGuid():N}.png");

            try
            {
                // Create a low-res image (300 x 200 px for A4 paper -> ~25 DPI)
                using (var bmp = new SKBitmap(300, 200))
                using (var canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(SKColors.White);
                    using var img = SKImage.FromBitmap(bmp);
                    using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                    using var fs = File.Create(smallImgPath);
                    data.SaveTo(fs);
                }

                var check = CertificateRenderer.CheckBackgroundDpi(smallImgPath, 297.0, 210.0);

                Assert.True(check.HasBackground);
                Assert.True(check.IsLowResolution);
                Assert.Contains("Resolusi Rendah", check.Message);
            }
            finally
            {
                if (File.Exists(smallImgPath)) File.Delete(smallImgPath);
            }
        }

        [Fact]
        public void FieldViewModel_AutoShrink_ShouldShrinkLongName()
        {
            var def = new TextFieldDefinition
            {
                Id = "nama",
                Text = "{nama}",
                FontFamily = "Arial",
                FontSize = 36.0,
                BoxWidth = 50.0, // narrow box to trigger shrink
                AutoShrink = true
            };

            var vm = new FieldViewModel(def);
            var rec = new RecipientRecord();
            rec.Data["nama"] = "Prof. Dr. Ir. H. Raden Muhammad Al-Fatih Kusuma Atmaja, S.Kom., M.Kom., Ph.D.";

            vm.UpdatePreviewText(rec);

            // Effective font size must be less than nominal font size (36.0)
            Assert.True(vm.EffectiveFontSize < 36.0, $"Expected shrunk font size (< 36), got {vm.EffectiveFontSize}");
            Assert.True(vm.EffectiveFontSize >= 6.0, "Font size should not shrink below 6pt minimum");
        }

        [Fact]
        public void FieldViewModel_CoordinateConversion_ShouldBeExact()
        {
            var def = new TextFieldDefinition
            {
                X = 148.5,
                Y = 105.0,
                BoxWidth = 200.0
            };

            var vm = new FieldViewModel(def);

            // Test roundtrip
            Assert.Equal(148.5, vm.X);
            Assert.Equal(105.0, vm.Y);
            Assert.Equal(200.0, vm.BoxWidth);

            // Moving in canvas pixels (DIP)
            double originalLeft = vm.CanvasLeft;
            vm.CanvasLeft = originalLeft + 37.79527559; // ~10 mm in 96 DPI
            Assert.True(Math.Abs(vm.X - 158.5) < 0.05, $"Expected ~158.5 mm, got {vm.X}");
        }

        [Fact]
        public void MainViewModel_CenterFieldHorizontally_ShouldCalculateExactCenterCoordinate()
        {
            var vm = new MainViewModel();
            // A4 Landscape is 297mm wide
            Assert.Equal(297.0, vm.WidthMm);

            var field = vm.AddNewField("Tes Center");
            field.BoxWidth = 200.0;
            field.X = 10.0;

            vm.CenterFieldHorizontally(field);

            // (297 - 200) / 2 = 48.5 mm
            Assert.Equal(48.5, field.X);

            field.BoxWidth = 150.0;
            vm.CenterFieldHorizontally(field);
            // (297 - 150) / 2 = 73.5 mm
            Assert.Equal(73.5, field.X);
        }

        [Fact]
        public void MainViewModel_SnapToCenterToggle_ShouldUpdateState()
        {
            var vm = new MainViewModel();
            Assert.True(vm.SnapToCenterEnabled);

            vm.SnapToCenterEnabled = false;
            Assert.False(vm.SnapToCenterEnabled);

            vm.SnapToCenterEnabled = true;
            Assert.True(vm.SnapToCenterEnabled);
        }

        [Fact]
        public void MainViewModel_NudgeField_ShouldMoveCorrectlyWithArrowSteps()
        {
            var vm = new MainViewModel();
            var field = vm.AddNewField("Tes Nudge");
            field.X = 50.0;
            field.Y = 50.0;

            // Geser Kiri (Left arrow) - 1.0 mm
            vm.NudgeField(field, -1.0, 0);
            Assert.Equal(49.0, field.X);
            Assert.Equal(50.0, field.Y);

            // Geser Kanan (Right arrow) + 1.0 mm
            vm.NudgeField(field, 1.0, 0);
            Assert.Equal(50.0, field.X);

            // Geser Atas (Up arrow) - 1.0 mm
            vm.NudgeField(field, 0, -1.0);
            Assert.Equal(49.0, field.Y);

            // Geser Bawah (Down arrow) + 1.0 mm
            vm.NudgeField(field, 0, 1.0);
            Assert.Equal(50.0, field.Y);

            // Shift + Arrow (5.0 mm fast step)
            vm.NudgeField(field, 5.0, 5.0);
            Assert.Equal(55.0, field.X);
            Assert.Equal(55.0, field.Y);

            // Boundary test: move left past 0
            vm.NudgeField(field, -100.0, 0);
            Assert.Equal(0.0, field.X);
        }

        [Fact]
        public void MainViewModel_UndoRedo_ShouldRevertAndReapplyState()
        {
            var vm = new MainViewModel();
            int initialCount = vm.Fields.Count;

            // Tambah field baru (SaveUndoState otomatis dipanggil)
            var newField = vm.AddNewField("Field Undo Test");
            Assert.Equal(initialCount + 1, vm.Fields.Count);
            Assert.True(vm.CanUndo);

            // Undo (Ctrl+Z)
            vm.Undo();
            Assert.Equal(initialCount, vm.Fields.Count);
            Assert.True(vm.CanRedo);

            // Redo (Ctrl+Y)
            vm.Redo();
            Assert.Equal(initialCount + 1, vm.Fields.Count);
        }

        [Fact]
        public void MainViewModel_CopyPaste_ShouldDuplicateWithNewId()
        {
            var vm = new MainViewModel();
            var field = vm.Fields[0];
            field.Text = "Teks Copy Paste";
            field.FontSize = 28.0;
            vm.SelectedField = field;

            int initialCount = vm.Fields.Count;

            // Copy (Ctrl+C)
            vm.CopySelectedField();
            Assert.True(vm.HasClipboardField);

            // Paste (Ctrl+V)
            vm.PasteField();
            Assert.Equal(initialCount + 1, vm.Fields.Count);

            var pasted = vm.SelectedField;
            Assert.NotNull(pasted);
            Assert.NotEqual(field.Id, pasted.Id);
            Assert.Equal("Teks Copy Paste", pasted.Text);
            Assert.Equal(28.0, pasted.FontSize);
        }

        [Fact]
        public void MainViewModel_CutPaste_ShouldRemoveAndRestoreField()
        {
            var vm = new MainViewModel();
            var field = vm.Fields[0];
            string origId = field.Id;
            vm.SelectedField = field;
            int initialCount = vm.Fields.Count;

            // Cut (Ctrl+X)
            vm.CutSelectedField();
            Assert.Equal(initialCount - 1, vm.Fields.Count);
            Assert.DoesNotContain(vm.Fields, f => f.Id == origId);

            // Paste (Ctrl+V)
            vm.PasteField();
            Assert.Equal(initialCount, vm.Fields.Count);
        }

        [Fact]
        public void MainViewModel_Duplicate_ShouldCreateCloneWithOffset()
        {
            var vm = new MainViewModel();
            var field = vm.Fields[0];
            double origX = field.X;
            double origY = field.Y;
            vm.SelectedField = field;
            int initialCount = vm.Fields.Count;

            // Duplicate (Ctrl+D)
            vm.DuplicateField(field);
            Assert.Equal(initialCount + 1, vm.Fields.Count);

            var duplicated = vm.SelectedField;
            Assert.NotNull(duplicated);
            Assert.NotEqual(field.Id, duplicated.Id);
            Assert.True(duplicated.X > origX);
            Assert.True(duplicated.Y > origY);
        }

        [Fact]
        public void CertificateTemplate_ExtractVariables_ShouldFindAllPlaceholders()
        {
            var tmpl = new CertificateTemplate();
            tmpl.Fields.Add(new TextFieldDefinition { Text = "Diberikan kepada {nama} ({nomor})" });
            tmpl.Fields.Add(new TextFieldDefinition { Text = "Sebagai {peran} pada acara {kegiatan}" });
            tmpl.Fields.Add(new TextFieldDefinition { Text = "Dari instansi {instansi} pada tanggal {tanggal}" });

            var vars = tmpl.ExtractVariables();

            Assert.Contains("nama", vars);
            Assert.Contains("nomor", vars);
            Assert.Contains("peran", vars);
            Assert.Contains("kegiatan", vars);
            Assert.Contains("instansi", vars);
            Assert.Contains("tanggal", vars);
            Assert.Equal("nama", vars[0]); // nama diprioritaskan di kolom pertama
        }

        [Fact]
        public void DataImportService_GenerateExcelTemplate_ShouldCreateValidExcelAndBeReadable()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_template_{Guid.NewGuid():N}.xlsx");

            try
            {
                var cols = new List<string> { "nama", "nomor", "peran", "instansi", "tanggal" };
                DataImportService.GenerateExcelTemplate(tempFile, cols);

                Assert.True(File.Exists(tempFile));
                var fileInfo = new FileInfo(tempFile);
                Assert.True(fileInfo.Length > 2000, "Excel template file must have substantial size.");

                // Verify file can be read back by ClosedXML
                using var wb = new XLWorkbook(tempFile);
                Assert.Equal(2, wb.Worksheets.Count);
                Assert.Equal("Data Penerima", wb.Worksheet(1).Name);
                Assert.Equal("Panduan Pengisian", wb.Worksheet(2).Name);

                // Verify columns in first sheet
                var ws = wb.Worksheet(1);
                for (int i = 0; i < cols.Count; i++)
                {
                    Assert.Equal(cols[i], ws.Cell(1, i + 1).GetString());
                }

                // Verify ImportFromExcel can import it seamlessly
                var importResult = DataImportService.ImportFromExcel(tempFile);
                Assert.True(importResult.Success);
                Assert.Equal(3, importResult.Records.Count);
                Assert.Equal("Budi Santoso, S.Kom.", importResult.Records[0].GetValue("nama"));
                Assert.Equal("Dr. Ir. H. Muhammad Al-Fatih, M.Kom.", importResult.Records[1].GetValue("nama"));
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void DataImportService_GenerateCsvTemplate_ShouldCreateValidCsv()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_template_{Guid.NewGuid():N}.csv");

            try
            {
                var cols = new List<string> { "nama", "nomor", "peran", "kegiatan" };
                DataImportService.GenerateCsvTemplate(tempFile, cols);

                Assert.True(File.Exists(tempFile));
                var res = DataImportService.ImportFromCsv(tempFile);
                Assert.True(res.Success);
                Assert.Equal(2, res.Records.Count);
                Assert.Equal("Budi Santoso, S.Kom.", res.Records[0].GetValue("nama"));
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void DataImportService_GenerateMockRecipients_ShouldCreateAccurateRecords()
        {
            var cols = new List<string> { "nama", "nomor", "peran", "instansi" };
            var list = DataImportService.GenerateMockRecipients(25, cols);

            Assert.Equal(25, list.Count);
            foreach (var rec in list)
            {
                Assert.False(string.IsNullOrEmpty(rec.GetValue("nama")));
                Assert.False(string.IsNullOrEmpty(rec.GetValue("nomor")));
                Assert.False(string.IsNullOrEmpty(rec.GetValue("peran")));
                Assert.False(string.IsNullOrEmpty(rec.GetValue("instansi")));
            }
        }

        [Theory]
        [InlineData("Dr. Ir. H. Muhammad Al-Fatih, M.Kom.", "Dr. Ir. H. Muhammad Al-Fatih, M.Kom")]
        [InlineData("Peserta/001:Test*File?Name", "Peserta_001_Test_File_Name")]
        [InlineData("   ", "sertifikat")]
        [InlineData("Trailing Dots....", "Trailing Dots")]
        [InlineData("Trailing Spaces   ", "Trailing Spaces")]
        public void ExportService_SanitizeFilename_ShouldSanitizeCorrectly(string input, string expected)
        {
            string actual = ExportService.SanitizeFilename(input);
            Assert.Equal(expected, actual);
            Assert.False(actual.EndsWith('.'));
            Assert.False(actual.EndsWith(' '));
        }

        [Fact]
        public void DataImportService_ImportFromCsv_EmptyFile_ShouldFailGracefully()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}.csv");
            try
            {
                File.WriteAllText(tempFile, "");
                var res = DataImportService.ImportFromCsv(tempFile);
                Assert.False(res.Success);
                Assert.Contains("kosong", res.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void TemplateService_SaveTemplate_ShouldPreserveRuntimeBackgroundPath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"tmpl_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            string jsonPath = Path.Combine(tempDir, "template.json");
            string sourceBg = Path.Combine(tempDir, "original_bg.png");
            File.WriteAllText(sourceBg, "dummy png content");

            try
            {
                var tmpl = new CertificateTemplate
                {
                    Background = sourceBg,
                    PageSize = "A4-landscape"
                };

                TemplateService.SaveTemplate(tmpl, jsonPath, true);

                // Runtime template should still point to valid full path
                Assert.True(File.Exists(tmpl.Background));
                Assert.True(Path.IsPathRooted(tmpl.Background));

                // Saved JSON should contain relative path
                string jsonText = File.ReadAllText(jsonPath);
                Assert.Contains("\"background\": \"original_bg.png\"", jsonText);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FontService_GetSkiaTypeface_ShouldCacheInstances()
        {
            var tf1 = FontService.GetSkiaTypeface("Arial", true, false);
            var tf2 = FontService.GetSkiaTypeface("Arial", true, false);

            Assert.NotNull(tf1);
            Assert.Same(tf1, tf2);
        }

        [Fact]
        public void CertificateRenderer_ExportSingleToPdf_WithZeroDimensions_ShouldHandleSafely()
        {
            string tempPdf = Path.Combine(Path.GetTempPath(), $"test_zero_{Guid.NewGuid():N}.pdf");
            try
            {
                var tmpl = new CertificateTemplate
                {
                    PageSize = "Custom",
                    CustomWidthMm = 0,
                    CustomHeightMm = -50,
                    Fields = new List<TextFieldDefinition>
                    {
                        new TextFieldDefinition { Text = "{nama}", FontSize = 0, BoxWidth = -10 }
                    }
                };

                var rec = new RecipientRecord();
                rec.Data["nama"] = "Test Name";

                // Should not throw exception
                CertificateRenderer.ExportSingleToPdf(tempPdf, tmpl, rec);
                Assert.True(File.Exists(tempPdf));
            }
            finally
            {
                if (File.Exists(tempPdf)) File.Delete(tempPdf);
            }
        }

        [Fact]
        public void IconsXaml_ShouldLoadAllGeometriesSuccessfully()
        {
            string xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Resources", "Icons.xaml");
            xamlPath = Path.GetFullPath(xamlPath);
            Assert.True(File.Exists(xamlPath), $"Icons.xaml not found at {xamlPath}");

            using var stream = File.OpenRead(xamlPath);
            var dict = (System.Windows.ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);
            Assert.NotNull(dict);
            Assert.NotEmpty(dict.Keys);

            foreach (var key in dict.Keys)
            {
                var geom = dict[key] as System.Windows.Media.Geometry;
                Assert.NotNull(geom);
                Assert.False(geom.IsEmpty());
            }
        }
    }
}
