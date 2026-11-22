using System.Text.Json.Serialization;

namespace CetakSertifikatApp.Models
{
    public class TextFieldDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "field";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "{nama}";

        [JsonPropertyName("fontFamily")]
        public string FontFamily { get; set; } = "Arial";

        [JsonPropertyName("fontSize")]
        public double FontSize { get; set; } = 32.0;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#1A1A1A";

        [JsonPropertyName("align")]
        public string Align { get; set; } = "center"; // "left", "center", "right"

        [JsonPropertyName("x")]
        public double X { get; set; } = 50.0; // in mm

        [JsonPropertyName("y")]
        public double Y { get; set; } = 90.0; // in mm

        [JsonPropertyName("boxWidth")]
        public double BoxWidth { get; set; } = 200.0; // in mm

        [JsonPropertyName("autoShrink")]
        public bool AutoShrink { get; set; } = true;

        [JsonPropertyName("isBold")]
        public bool IsBold { get; set; } = false;

        [JsonPropertyName("isItalic")]
        public bool IsItalic { get; set; } = false;

        public TextFieldDefinition Clone()
        {
            return new TextFieldDefinition
            {
                Id = this.Id,
                Text = this.Text,
                FontFamily = this.FontFamily,
                FontSize = this.FontSize,
                Color = this.Color,
                Align = this.Align,
                X = this.X,
                Y = this.Y,
                BoxWidth = this.BoxWidth,
                AutoShrink = this.AutoShrink,
                IsBold = this.IsBold,
                IsItalic = this.IsItalic
            };
        }
    }
}
