using System;
using System.Collections.Generic;

namespace CetakSertifikatApp.Models
{
    public class PageSizePreset
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }

        public static List<PageSizePreset> Presets => new()
        {
            new PageSizePreset { Name = "A4-landscape", DisplayName = "A4 Lanskap (297 x 210 mm)", WidthMm = 297.0, HeightMm = 210.0 },
            new PageSizePreset { Name = "A4-portrait", DisplayName = "A4 Potret (210 x 297 mm)", WidthMm = 210.0, HeightMm = 297.0 },
            new PageSizePreset { Name = "Letter-landscape", DisplayName = "Letter Lanskap (279.4 x 215.9 mm)", WidthMm = 279.4, HeightMm = 215.9 },
            new PageSizePreset { Name = "Letter-portrait", DisplayName = "Letter Potret (215.9 x 279.4 mm)", WidthMm = 215.9, HeightMm = 279.4 },
            new PageSizePreset { Name = "Custom", DisplayName = "Ukuran Kustom...", WidthMm = 297.0, HeightMm = 210.0 }
        };
    }
}
