using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using CetakSertifikatApp.Models;

namespace CetakSertifikatApp.ViewModels
{
    public class FieldViewModel : INotifyPropertyChanged
    {
        public const double MmToDip = 96.0 / 25.4; // 3.779527559 DIP per mm

        private readonly TextFieldDefinition _model;
        private bool _isSelected;
        private string _previewText = string.Empty;
        private double _effectiveFontSize;

        public FieldViewModel(TextFieldDefinition model)
        {
            _model = model;
            _effectiveFontSize = model.FontSize;
        }

        public TextFieldDefinition Model => _model;

        public string Id
        {
            get => _model.Id;
            set
            {
                if (_model.Id != value)
                {
                    _model.Id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Text
        {
            get => _model.Text;
            set
            {
                if (_model.Text != value)
                {
                    _model.Text = value;
                    OnPropertyChanged();
                    UpdatePreviewText(null);
                }
            }
        }

        public string FontFamily
        {
            get => _model.FontFamily;
            set
            {
                if (_model.FontFamily != value)
                {
                    _model.FontFamily = value;
                    OnPropertyChanged();
                    UpdatePreviewText(null);
                }
            }
        }

        public double FontSize
        {
            get => _model.FontSize;
            set
            {
                if (Math.Abs(_model.FontSize - value) > 0.01)
                {
                    _model.FontSize = Math.Max(6, value);
                    OnPropertyChanged();
                    UpdatePreviewText(null);
                }
            }
        }

        public string Color
        {
            get => _model.Color;
            set
            {
                if (_model.Color != value)
                {
                    _model.Color = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Brush));
                }
            }
        }

        public SolidColorBrush Brush
        {
            get
            {
                try
                {
                    return (SolidColorBrush)new BrushConverter().ConvertFromString(Color)!;
                }
                catch
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                }
            }
        }

        public string Align
        {
            get => _model.Align;
            set
            {
                if (_model.Align != value)
                {
                    _model.Align = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextAlignment));
                }
            }
        }

        public TextAlignment TextAlignment
        {
            get => Align?.ToLowerInvariant() switch
            {
                "center" => TextAlignment.Center,
                "right" => TextAlignment.Right,
                _ => TextAlignment.Left
            };
            set
            {
                Align = value switch
                {
                    TextAlignment.Center => "center",
                    TextAlignment.Right => "right",
                    _ => "left"
                };
            }
        }

        public double X
        {
            get => _model.X;
            set
            {
                double rounded = Math.Round(value, 2);
                if (Math.Abs(_model.X - rounded) > 0.001)
                {
                    _model.X = rounded;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanvasLeft));
                }
            }
        }

        public double Y
        {
            get => _model.Y;
            set
            {
                double rounded = Math.Round(value, 2);
                if (Math.Abs(_model.Y - rounded) > 0.001)
                {
                    _model.Y = rounded;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanvasTop));
                }
            }
        }

        public double BoxWidth
        {
            get => _model.BoxWidth;
            set
            {
                double rounded = Math.Round(Math.Max(10, value), 2);
                if (Math.Abs(_model.BoxWidth - rounded) > 0.001)
                {
                    _model.BoxWidth = rounded;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanvasWidth));
                    UpdatePreviewText(null);
                }
            }
        }

        public bool AutoShrink
        {
            get => _model.AutoShrink;
            set
            {
                if (_model.AutoShrink != value)
                {
                    _model.AutoShrink = value;
                    OnPropertyChanged();
                    UpdatePreviewText(null);
                }
            }
        }

        public bool IsBold
        {
            get => _model.IsBold;
            set
            {
                if (_model.IsBold != value)
                {
                    _model.IsBold = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FontWeight));
                    UpdatePreviewText(null);
                }
            }
        }

        public FontWeight FontWeight => IsBold ? FontWeights.Bold : FontWeights.Normal;

        public bool IsItalic
        {
            get => _model.IsItalic;
            set
            {
                if (_model.IsItalic != value)
                {
                    _model.IsItalic = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FontStyle));
                    UpdatePreviewText(null);
                }
            }
        }

        public FontStyle FontStyle => IsItalic ? FontStyles.Italic : FontStyles.Normal;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PreviewText
        {
            get => _previewText;
            private set
            {
                if (_previewText != value)
                {
                    _previewText = value;
                    OnPropertyChanged();
                }
            }
        }

        public double EffectiveFontSize
        {
            get => _effectiveFontSize;
            private set
            {
                if (Math.Abs(_effectiveFontSize - value) > 0.1)
                {
                    _effectiveFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        // Canvas DIP Coordinates
        public double CanvasLeft
        {
            get => X * MmToDip;
            set => X = value / MmToDip;
        }

        public double CanvasTop
        {
            get => Y * MmToDip;
            set => Y = value / MmToDip;
        }

        public double CanvasWidth
        {
            get => BoxWidth * MmToDip;
            set => BoxWidth = value / MmToDip;
        }

        private RecipientRecord? _lastRecipient;

        public void UpdatePreviewText(RecipientRecord? recipient)
        {
            if (recipient != null) _lastRecipient = recipient;
            recipient ??= _lastRecipient;

            string raw = recipient != null ? recipient.ReplacePlaceholders(Text) : Text;
            PreviewText = raw;

            // Compute auto-shrink in WPF DIP
            if (AutoShrink && BoxWidth > 0 && !string.IsNullOrEmpty(raw))
            {
                double targetBoxDip = CanvasWidth;
                double nominalSize = FontSize * (96.0 / 72.0); // convert pt to DIP

                var typeface = new Typeface(
                    new System.Windows.Media.FontFamily(FontFamily),
                    FontStyle,
                    FontWeight,
                    FontStretches.Normal);

                var formattedText = new FormattedText(
                    raw,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    nominalSize,
                    System.Windows.Media.Brushes.Black,
                    1.0);

                if (formattedText.Width > targetBoxDip && targetBoxDip > 0)
                {
                    double factor = targetBoxDip / formattedText.Width;
                    EffectiveFontSize = Math.Max(FontSize * factor, 6.0);
                }
                else
                {
                    EffectiveFontSize = FontSize;
                }
            }
            else
            {
                EffectiveFontSize = FontSize;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
