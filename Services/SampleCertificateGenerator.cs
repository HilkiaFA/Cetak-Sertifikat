using System;
using System.IO;
using SkiaSharp;

namespace CetakSertifikatApp.Services
{
    public static class SampleCertificateGenerator
    {
        public static string EnsureSampleBackgroundExists()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string bgPath = Path.Combine(appDir, "sample_certificate_bg.png");

            if (File.Exists(bgPath)) return bgPath;

            try
            {
                // Generate a 300 DPI A4 Landscape image: 3508 x 2480 px
                int width = 3508;
                int height = 2480;

                using var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);

                // 1. Warm cream background
                canvas.Clear(new SKColor(253, 252, 248));

                // 2. Decorative outer border (Navy Blue #0F2027)
                using (var outerBorder = new SKPaint
                {
                    Color = new SKColor(15, 32, 67),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 40,
                    IsAntialias = true
                })
                {
                    canvas.DrawRect(60, 60, width - 120, height - 120, outerBorder);
                }

                // 3. Decorative inner thin border (Gold #C5A059)
                using (var innerBorder = new SKPaint
                {
                    Color = new SKColor(197, 160, 89),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 12,
                    IsAntialias = true
                })
                {
                    canvas.DrawRect(110, 110, width - 220, height - 220, innerBorder);
                }

                // 4. Subtle corner ornaments
                using (var cornerPaint = new SKPaint
                {
                    Color = new SKColor(197, 160, 89),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    float sz = 80;
                    // Top-left
                    canvas.DrawRect(110, 110, sz, sz, cornerPaint);
                    // Top-right
                    canvas.DrawRect(width - 110 - sz, 110, sz, sz, cornerPaint);
                    // Bottom-left
                    canvas.DrawRect(110, height - 110 - sz, sz, sz, cornerPaint);
                    // Bottom-right
                    canvas.DrawRect(width - 110 - sz, height - 110 - sz, sz, sz, cornerPaint);
                }

                // 5. Header text "SERTIFIKAT PENGHARGAAN"
                using (var headerPaint = new SKPaint
                {
                    Color = new SKColor(15, 32, 67),
                    IsAntialias = true
                })
                using (var font = new SKFont(SKTypeface.FromFamilyName("Times New Roman", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 110))
                {
                    canvas.DrawText("SERTIFIKAT PENGHARGAAN", width / 2f, 420, SKTextAlign.Center, font, headerPaint);
                }

                // 6. Subtitle "DIBERIKAN DENGAN BANGGA KEPADA:"
                using (var subPaint = new SKPaint
                {
                    Color = new SKColor(140, 115, 65),
                    IsAntialias = true
                })
                using (var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 44))
                {
                    canvas.DrawText("DIBERIKAN KEPADA:", width / 2f, 540, SKTextAlign.Center, font, subPaint);
                }

                // 7. Decorative line under name area
                using (var linePaint = new SKPaint
                {
                    Color = new SKColor(197, 160, 89, 180),
                    StrokeWidth = 4,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(width * 0.2f, 1320, width * 0.8f, 1320, linePaint);
                }

                // 8. Bottom signature lines
                using (var sigLinePaint = new SKPaint
                {
                    Color = new SKColor(100, 100, 100),
                    StrokeWidth = 4,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(width * 0.15f, 2050, width * 0.40f, 2050, sigLinePaint);
                    canvas.DrawLine(width * 0.60f, 2050, width * 0.85f, 2050, sigLinePaint);
                }

                using (var labelPaint = new SKPaint
                {
                    Color = new SKColor(100, 100, 100),
                    IsAntialias = true
                })
                using (var font = new SKFont(SKTypeface.FromFamilyName("Arial"), 36))
                {
                    canvas.DrawText("Ketua Panitia Pelaksana", width * 0.275f, 2120, SKTextAlign.Center, font, labelPaint);
                    canvas.DrawText("Pimpinan Instansi / Direktur", width * 0.725f, 2120, SKTextAlign.Center, font, labelPaint);
                }

                // Save PNG
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.Create(bgPath);
                data.SaveTo(stream);

                return bgPath;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
