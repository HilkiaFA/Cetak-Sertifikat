using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SkiaSharp;

namespace CetakSertifikatApp.Services
{
    public static class FontService
    {
        private static List<string>? _installedFonts;
        private static readonly ConcurrentDictionary<(string, bool, bool), SKTypeface> _typefaceCache = new();

        public static List<string> GetInstalledFontFamilies()
        {
            if (_installedFonts != null) return _installedFonts;

            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (FontFamily font in Fonts.SystemFontFamilies)
                {
                    if (!string.IsNullOrWhiteSpace(font.Source))
                    {
                        list.Add(font.Source);
                    }
                }
            }
            catch
            {
                // Fallback standard fonts
                list.Add("Arial");
                list.Add("Calibri");
                list.Add("Times New Roman");
                list.Add("Georgia");
                list.Add("Segoe UI");
                list.Add("Garamond");
            }

            // Put preferred certificate fonts first if present, then sorted
            var priorityFonts = new[] { "Montserrat", "Arial", "Calibri", "Times New Roman", "Georgia", "Segoe UI", "Garamond", "Cambria", "Trebuchet MS", "Verdana", "Tahoma" };
            var result = new List<string>();

            foreach (var p in priorityFonts)
            {
                if (list.Contains(p))
                {
                    result.Add(p);
                    list.Remove(p);
                }
            }

            result.AddRange(list.OrderBy(x => x));
            _installedFonts = result;
            return _installedFonts;
        }

        public static SKTypeface GetSkiaTypeface(string familyName, bool isBold, bool isItalic)
        {
            string keyName = (familyName ?? "Arial").Trim();
            var key = (keyName, isBold, isItalic);

            return _typefaceCache.GetOrAdd(key, k =>
            {
                SKFontStyleWeight weight = k.Item2 ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
                SKFontStyleSlant slant = k.Item3 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
                var style = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);

                if (!string.IsNullOrWhiteSpace(k.Item1))
                {
                    var tf = SKTypeface.FromFamilyName(k.Item1, style);
                    if (tf != null) return tf;
                }

                return SKTypeface.FromFamilyName("Arial", style) ?? SKTypeface.Default;
            });
        }
    }
}
