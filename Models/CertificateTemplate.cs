using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CetakSertifikatApp.Models
{
    public class CertificateTemplate
    {
        [JsonPropertyName("background")]
        public string Background { get; set; } = string.Empty;

        [JsonPropertyName("pageSize")]
        public string PageSize { get; set; } = "A4-landscape";

        [JsonPropertyName("customWidthMm")]
        public double CustomWidthMm { get; set; } = 297.0;

        [JsonPropertyName("customHeightMm")]
        public double CustomHeightMm { get; set; } = 210.0;

        [JsonPropertyName("fields")]
        public List<TextFieldDefinition> Fields { get; set; } = new();

        public double GetWidthMm()
        {
            return PageSize switch
            {
                "A4-landscape" => 297.0,
                "A4-portrait" => 210.0,
                "Letter-landscape" => 279.4,
                "Letter-portrait" => 215.9,
                _ => CustomWidthMm > 0 ? CustomWidthMm : 297.0
            };
        }

        public double GetHeightMm()
        {
            return PageSize switch
            {
                "A4-landscape" => 210.0,
                "A4-portrait" => 297.0,
                "Letter-landscape" => 215.9,
                "Letter-portrait" => 279.4,
                _ => CustomHeightMm > 0 ? CustomHeightMm : 210.0
            };
        }

        public List<string> ExtractVariables()
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Text)) continue;
                int start = 0;
                while ((start = field.Text.IndexOf('{', start)) != -1)
                {
                    int end = field.Text.IndexOf('}', start);
                    if (end != -1 && end > start + 1)
                    {
                        string varName = field.Text.Substring(start + 1, end - start - 1).Trim();
                        if (!string.IsNullOrWhiteSpace(varName))
                        {
                            list.Add(varName);
                        }
                        start = end + 1;
                    }
                    else
                    {
                        start++;
                    }
                }
            }

            if (list.Count == 0)
            {
                return new List<string> { "nama", "nomor", "peran", "kegiatan", "instansi", "tanggal" };
            }

            var ordered = new List<string>();
            if (list.Contains("nama"))
            {
                ordered.Add("nama");
                list.Remove("nama");
            }
            ordered.AddRange(list);
            return ordered;
        }
    }
}
