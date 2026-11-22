using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using SkiaSharp;
using CetakSertifikatApp.Models;

namespace CetakSertifikatApp.Services
{
    public class DpiCheckResult
    {
        public bool HasBackground { get; set; }
        public int WidthPx { get; set; }
        public int HeightPx { get; set; }
        public double DpiX { get; set; }
        public double DpiY { get; set; }
        public bool IsLowResolution => HasBackground && (DpiX < 150.0 || DpiY < 150.0);
        public string Message { get; set; } = string.Empty;
    }

    public static class CertificateRenderer
    {
        public const double MmToPoint = 72.0 / 25.4; // 2.83464567

        public static DpiCheckResult CheckBackgroundDpi(string? bgPath, double widthMm, double heightMm)
        {
            var res = new DpiCheckResult();
            if (string.IsNullOrWhiteSpace(bgPath) || !File.Exists(bgPath))
            {
                res.HasBackground = false;
                res.Message = "Belum ada gambar background yang dipilih.";
                return res;
            }

            try
            {
                using var codec = SKCodec.Create(bgPath);
                if (codec == null)
                {
                    res.Message = "Format gambar tidak didukung.";
                    return res;
                }

                res.HasBackground = true;
                res.WidthPx = codec.Info.Width;
                res.HeightPx = codec.Info.Height;

                double widthInches = widthMm / 25.4;
                double heightInches = heightMm / 25.4;

                res.DpiX = widthInches > 0 ? res.WidthPx / widthInches : 0;
                res.DpiY = heightInches > 0 ? res.HeightPx / heightInches : 0;

                if (res.IsLowResolution)
                {
                    res.Message = $"Resolusi Rendah (~{res.DpiX:F0} DPI). Standar cetak minimal 150 DPI agar tidak pecah/buram.";
                }
                else
                {
                    res.Message = $"Resolusi Baik (~{res.DpiX:F0} DPI). Siap cetak tajam.";
                }
            }
            catch (Exception ex)
            {
                res.Message = $"Gagal memeriksa resolusi gambar: {ex.Message}";
            }

            return res;
        }

        public static SKBitmap? LoadBitmap(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            try
            {
                return SKBitmap.Decode(path);
            }
            catch
            {
                return null;
            }
        }

        public static void RenderPage(
            SKCanvas canvas,
            CertificateTemplate template,
            RecipientRecord recipient,
            float pageWidthPt,
            float pageHeightPt,
            SKBitmap? cachedBg = null)
        {
            // 1. Draw Background
            if (cachedBg != null)
            {
                var destRect = new SKRect(0, 0, pageWidthPt, pageHeightPt);
                using var paint = new SKPaint
                {
                    IsAntialias = true
                };
                canvas.DrawBitmap(cachedBg, destRect, new SKSamplingOptions(SKFilterMode.Linear), paint);
            }
            else
            {
                // Default plain white background
                canvas.Clear(SKColors.White);
            }

            // 2. Draw each text field
            foreach (var field in template.Fields)
            {
                DrawTextField(canvas, field, recipient);
            }
        }

        private static void DrawTextField(SKCanvas canvas, TextFieldDefinition field, RecipientRecord recipient)
        {
            string resolvedText = recipient.ReplacePlaceholders(field.Text);
            if (string.IsNullOrEmpty(resolvedText)) return;

            // Coordinates in PDF points (72 DPI)
            float xPt = (float)(field.X * MmToPoint);
            float yPt = (float)(field.Y * MmToPoint);
            float boxWidthPt = Math.Max(0.0f, (float)(field.BoxWidth * MmToPoint));
            float nominalFontSize = Math.Max(1.0f, (float)field.FontSize);

            var typeface = FontService.GetSkiaTypeface(field.FontFamily, field.IsBold, field.IsItalic);

            // Auto-shrink calculation
            float effectiveFontSize = nominalFontSize;
            using (var testFont = new SKFont(typeface, nominalFontSize))
            {
                float measuredWidth = testFont.MeasureText(resolvedText);
                if (field.AutoShrink && boxWidthPt > 0 && measuredWidth > boxWidthPt)
                {
                    float factor = boxWidthPt / measuredWidth;
                    effectiveFontSize = Math.Max(nominalFontSize * factor, 6.0f);
                }
            }

            using var font = new SKFont(typeface, effectiveFontSize)
            {
                Subpixel = true
            };
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ParseColor(field.Color)
            };

            // Calculate baseline Y
            font.GetFontMetrics(out var metrics);
            float baselineY = yPt - metrics.Ascent;

            // Alignment
            SKTextAlign align;
            float drawX;

            switch (field.Align?.ToLowerInvariant())
            {
                case "center":
                    align = SKTextAlign.Center;
                    drawX = xPt + (boxWidthPt / 2.0f);
                    break;
                case "right":
                    align = SKTextAlign.Right;
                    drawX = xPt + boxWidthPt;
                    break;
                default:
                    align = SKTextAlign.Left;
                    drawX = xPt;
                    break;
            }

            canvas.DrawText(resolvedText, drawX, baselineY, align, font, paint);
        }

        public static SKColor ParseColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return new SKColor(26, 26, 26);
            hex = hex.Trim();
            if (SKColor.TryParse(hex, out var color)) return color;
            return new SKColor(26, 26, 26);
        }

        public static BitmapSource RenderToWpfBitmap(CertificateTemplate template, RecipientRecord recipient, int dpi = 150)
        {
            double widthMm = Math.Max(10.0, template.GetWidthMm());
            double heightMm = Math.Max(10.0, template.GetHeightMm());

            // Calculate pixel dimensions
            int widthPx = Math.Max(1, (int)Math.Round((widthMm / 25.4) * dpi));
            int heightPx = Math.Max(1, (int)Math.Round((heightMm / 25.4) * dpi));

            float scale = (float)dpi / 72.0f;
            float widthPt = (float)(widthMm * MmToPoint);
            float heightPt = (float)(heightMm * MmToPoint);

            using var bitmap = new SKBitmap(widthPx, heightPx);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Scale(scale);
                using var bgBitmap = LoadBitmap(template.Background);
                RenderPage(canvas, template, recipient, widthPt, heightPt, bgBitmap);
            }

            // Convert SKBitmap to WPF BitmapSource via PNG stream (clean, leak-free, zero dependencies)
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 95);
            using var ms = new MemoryStream(data.ToArray());

            var bmpImage = new BitmapImage();
            bmpImage.BeginInit();
            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
            bmpImage.StreamSource = ms;
            bmpImage.EndInit();
            bmpImage.Freeze();

            return bmpImage;
        }

        public static void ExportToPdf(
            string outputPath,
            CertificateTemplate template,
            List<RecipientRecord> recipients,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            float widthPt = (float)(Math.Max(10.0, template.GetWidthMm()) * MmToPoint);
            float heightPt = (float)(Math.Max(10.0, template.GetHeightMm()) * MmToPoint);

            using var bgBitmap = LoadBitmap(template.Background);
            using var stream = File.Create(outputPath);
            using var document = SKDocument.CreatePdf(stream);

            int total = recipients.Count;
            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var recipient = recipients[i];
                using (var canvas = document.BeginPage(widthPt, heightPt))
                {
                    RenderPage(canvas, template, recipient, widthPt, heightPt, bgBitmap);
                    document.EndPage();
                }

                progress?.Report((int)(((i + 1) / (double)total) * 100));
            }

            document.Close();
        }

        public static void ExportSingleToPdf(
            string outputPath,
            CertificateTemplate template,
            RecipientRecord recipient,
            SKBitmap? cachedBg = null)
        {
            float widthPt = (float)(Math.Max(10.0, template.GetWidthMm()) * MmToPoint);
            float heightPt = (float)(Math.Max(10.0, template.GetHeightMm()) * MmToPoint);

            bool disposeBg = false;
            SKBitmap? bg = cachedBg;
            if (bg == null && !string.IsNullOrWhiteSpace(template.Background))
            {
                bg = LoadBitmap(template.Background);
                disposeBg = true;
            }

            try
            {
                using var stream = File.Create(outputPath);
                using var document = SKDocument.CreatePdf(stream);

                using (var canvas = document.BeginPage(widthPt, heightPt))
                {
                    RenderPage(canvas, template, recipient, widthPt, heightPt, bg);
                    document.EndPage();
                }

                document.Close();
            }
            finally
            {
                if (disposeBg && bg != null)
                {
                    bg.Dispose();
                }
            }
        }

        public static void ExportSingleToPng(
            string outputPath,
            CertificateTemplate template,
            RecipientRecord recipient,
            int dpi = 300,
            SKBitmap? cachedBg = null)
        {
            double widthMm = Math.Max(10.0, template.GetWidthMm());
            double heightMm = Math.Max(10.0, template.GetHeightMm());

            int widthPx = Math.Max(1, (int)Math.Round((widthMm / 25.4) * dpi));
            int heightPx = Math.Max(1, (int)Math.Round((heightMm / 25.4) * dpi));

            float scale = (float)dpi / 72.0f;
            float widthPt = (float)(widthMm * MmToPoint);
            float heightPt = (float)(heightMm * MmToPoint);

            bool disposeBg = false;
            SKBitmap? bg = cachedBg;
            if (bg == null && !string.IsNullOrWhiteSpace(template.Background))
            {
                bg = LoadBitmap(template.Background);
                disposeBg = true;
            }

            try
            {
                using var bitmap = new SKBitmap(widthPx, heightPx);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Scale(scale);
                    RenderPage(canvas, template, recipient, widthPt, heightPt, bg);
                }

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.Create(outputPath);
                data.SaveTo(stream);
            }
            finally
            {
                if (disposeBg && bg != null)
                {
                    bg.Dispose();
                }
            }
        }
    }
}
