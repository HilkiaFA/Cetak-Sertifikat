using System;
using System.IO;
using System.Text.Json;
using CetakSertifikatApp.Models;

namespace CetakSertifikatApp.Services
{
    public static class TemplateService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static void SaveTemplate(CertificateTemplate template, string filePath, bool copyBackgroundToFolder = true)
        {
            string? templateDir = Path.GetDirectoryName(filePath);
            string originalBg = template.Background;
            string? targetBgPath = null;

            try
            {
                if (copyBackgroundToFolder && !string.IsNullOrWhiteSpace(template.Background) && File.Exists(template.Background))
                {
                    if (!string.IsNullOrEmpty(templateDir))
                    {
                        string bgFileName = Path.GetFileName(template.Background);
                        targetBgPath = Path.Combine(templateDir, bgFileName);

                        if (!string.Equals(template.Background, targetBgPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(template.Background, targetBgPath, true);
                        }
                        // Store relative filename in JSON as specified in prompt: "background": "bg.png"
                        template.Background = bgFileName;
                    }
                }

                string json = JsonSerializer.Serialize(template, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            finally
            {
                // Preserve valid full path in the runtime template so the app canvas doesn't lose the image
                template.Background = targetBgPath ?? originalBg;
            }
        }

        public static CertificateTemplate LoadTemplate(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var template = JsonSerializer.Deserialize<CertificateTemplate>(json, JsonOptions) ?? new CertificateTemplate();

            string? templateDir = Path.GetDirectoryName(filePath);

            // Resolve relative background path
            if (!string.IsNullOrWhiteSpace(template.Background) && !Path.IsPathRooted(template.Background) && !string.IsNullOrEmpty(templateDir))
            {
                string candidate = Path.Combine(templateDir, template.Background);
                if (File.Exists(candidate))
                {
                    template.Background = candidate;
                }
            }

            return template;
        }

        public static CertificateTemplate CreateDefaultTemplate(string? bgPath = null)
        {
            var template = new CertificateTemplate
            {
                Background = bgPath ?? string.Empty,
                PageSize = "A4-landscape",
                CustomWidthMm = 297.0,
                CustomHeightMm = 210.0,
                Fields = new()
                {
                    new TextFieldDefinition
                    {
                        Id = "nomor",
                        Text = "No: {nomor}",
                        FontFamily = "Arial",
                        FontSize = 14.0,
                        Color = "#555555",
                        Align = "center",
                        X = 88.5,
                        Y = 56.0,
                        BoxWidth = 120.0,
                        AutoShrink = false
                    },
                    new TextFieldDefinition
                    {
                        Id = "nama",
                        Text = "{nama}",
                        FontFamily = "Arial",
                        FontSize = 34.0,
                        Color = "#0F2027",
                        Align = "center",
                        X = 38.5,
                        Y = 88.0,
                        BoxWidth = 220.0,
                        AutoShrink = true,
                        IsBold = true
                    },
                    new TextFieldDefinition
                    {
                        Id = "peran",
                        Text = "Sebagai {peran}",
                        FontFamily = "Arial",
                        FontSize = 18.0,
                        Color = "#8C7341",
                        Align = "center",
                        X = 48.5,
                        Y = 118.0,
                        BoxWidth = 200.0,
                        AutoShrink = true,
                        IsItalic = true
                    },
                    new TextFieldDefinition
                    {
                        Id = "kegiatan",
                        Text = "Atas partisipasinya dalam kegiatan {kegiatan}",
                        FontFamily = "Arial",
                        FontSize = 15.0,
                        Color = "#333333",
                        Align = "center",
                        X = 38.5,
                        Y = 132.0,
                        BoxWidth = 220.0,
                        AutoShrink = true
                    },
                    new TextFieldDefinition
                    {
                        Id = "tanggal",
                        Text = "Jakarta, {tanggal}",
                        FontFamily = "Arial",
                        FontSize = 14.0,
                        Color = "#555555",
                        Align = "center",
                        X = 98.5,
                        Y = 165.0,
                        BoxWidth = 100.0,
                        AutoShrink = false
                    }
                }
            };

            return template;
        }
    }
}
