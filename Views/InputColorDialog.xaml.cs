using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CetakSertifikatApp.Views
{
    public partial class InputColorDialog : Window
    {
        private bool _isUpdatingFromSliders = false;
        private bool _isUpdatingFromHex = false;

        public string SelectedHexColor { get; private set; } = "#1A1A1A";

        public InputColorDialog(string initialHex)
        {
            InitializeComponent();
            SetColor(initialHex);
        }

        private void SetColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) hex = "#1A1A1A";
            hex = hex.Trim();
            if (!hex.StartsWith("#")) hex = "#" + hex;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                SelectedHexColor = hex;
                TxtHex.Text = hex;
                ColorPreview.Background = new SolidColorBrush(color);

                _isUpdatingFromHex = true;
                SliderR.Value = color.R;
                SliderG.Value = color.G;
                SliderB.Value = color.B;
                TxtValR.Text = color.R.ToString();
                TxtValG.Text = color.G.ToString();
                TxtValB.Text = color.B.ToString();
                _isUpdatingFromHex = false;
            }
            catch
            {
                // Ignore invalid hex while typing
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingFromHex) return;

            _isUpdatingFromSliders = true;
            byte r = (byte)SliderR.Value;
            byte g = (byte)SliderG.Value;
            byte b = (byte)SliderB.Value;

            TxtValR.Text = r.ToString();
            TxtValG.Text = g.ToString();
            TxtValB.Text = b.ToString();

            var color = Color.FromRgb(r, g, b);
            ColorPreview.Background = new SolidColorBrush(color);

            string hex = $"#{r:X2}{g:X2}{b:X2}";
            SelectedHexColor = hex;
            TxtHex.Text = hex;
            _isUpdatingFromSliders = false;
        }

        private void TxtHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromSliders) return;
            string text = TxtHex.Text.Trim();
            if (!text.StartsWith("#")) text = "#" + text;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(text);
                ColorPreview.Background = new SolidColorBrush(color);
                SelectedHexColor = text;

                _isUpdatingFromHex = true;
                SliderR.Value = color.R;
                SliderG.Value = color.G;
                SliderB.Value = color.B;
                TxtValR.Text = color.R.ToString();
                TxtValG.Text = color.G.ToString();
                TxtValB.Text = color.B.ToString();
                _isUpdatingFromHex = false;
            }
            catch
            {
            }
        }

        private void Palette_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                SetColor(hex);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
