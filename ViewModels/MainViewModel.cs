using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using CetakSertifikatApp.Models;
using CetakSertifikatApp.Services;

namespace CetakSertifikatApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private CertificateTemplate _template;
        private FieldViewModel? _selectedField;
        private List<RecipientRecord> _recipients = new();
        private int _currentRecipientIndex = 0;
        private PageSizePreset _selectedPageSize;
        private double _customWidthMm = 297.0;
        private double _customHeightMm = 210.0;
        private string? _backgroundPath;
        private BitmapSource? _backgroundImageSource;
        private DpiCheckResult _dpiInfo = new();
        private double _zoom = 0.85; // Default comfortable view
        private string _statusMessage = "Siap. Unggah background atau pilih template untuk memulai.";

        public MainViewModel()
        {
            // Default template
            _template = TemplateService.CreateDefaultTemplate();
            _selectedPageSize = PageSizePreset.Presets[0];

            Fields = new ObservableCollection<FieldViewModel>();
            AvailableVariables = new ObservableCollection<string> { "nama", "nomor", "peran", "kegiatan", "instansi", "tanggal" };
            InstalledFonts = FontService.GetInstalledFontFamilies();

            // Load default sample recipients
            _recipients = DataImportService.GetSampleRecipients();

            // Populate fields
            LoadFieldsFromTemplate(_template);

            // Check sample background
            string sampleBg = SampleCertificateGenerator.EnsureSampleBackgroundExists();
            if (File.Exists(sampleBg))
            {
                SetBackground(sampleBg);
            }
        }

        public CertificateTemplate Template => _template;

        public ObservableCollection<FieldViewModel> Fields { get; }
        public ObservableCollection<string> AvailableVariables { get; }
        public List<string> InstalledFonts { get; }
        public List<PageSizePreset> PageSizes => PageSizePreset.Presets;

        public FieldViewModel? SelectedField
        {
            get => _selectedField;
            set
            {
                if (_selectedField != value)
                {
                    if (_selectedField != null) _selectedField.IsSelected = false;
                    _selectedField = value;
                    if (_selectedField != null) _selectedField.IsSelected = true;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedField));
                }
            }
        }

        public bool HasSelectedField => SelectedField != null;

        public List<RecipientRecord> Recipients
        {
            get => _recipients;
            set
            {
                _recipients = value ?? new List<RecipientRecord>();
                _currentRecipientIndex = 0;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalRecipients));
                OnPropertyChanged(nameof(CurrentRecipientIndexDisplay));
                OnPropertyChanged(nameof(CurrentRecipient));
                OnPropertyChanged(nameof(HasRecipients));
                UpdateAvailableVariables();
                RefreshFieldPreviews();
            }
        }

        public int CurrentRecipientIndex
        {
            get => _currentRecipientIndex;
            set
            {
                if (value >= 0 && value < TotalRecipients && _currentRecipientIndex != value)
                {
                    _currentRecipientIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentRecipientIndexDisplay));
                    OnPropertyChanged(nameof(CurrentRecipient));
                    RefreshFieldPreviews();
                }
            }
        }

        public int CurrentRecipientIndexDisplay => TotalRecipients > 0 ? _currentRecipientIndex + 1 : 0;
        public int TotalRecipients => _recipients.Count;
        public bool HasRecipients => TotalRecipients > 0;

        public RecipientRecord CurrentRecipient =>
            (TotalRecipients > 0 && _currentRecipientIndex >= 0 && _currentRecipientIndex < TotalRecipients)
                ? _recipients[_currentRecipientIndex]
                : new RecipientRecord { RowIndex = 1, Data = new() { { "nama", "Nama Peserta" } } };

        public PageSizePreset SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                if (_selectedPageSize != value && value != null)
                {
                    _selectedPageSize = value;
                    _template.PageSize = value.Name;
                    if (value.Name != "Custom")
                    {
                        _customWidthMm = value.WidthMm;
                        _customHeightMm = value.HeightMm;
                        _template.CustomWidthMm = value.WidthMm;
                        _template.CustomHeightMm = value.HeightMm;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCustomPageSize));
                    OnPropertyChanged(nameof(WidthMm));
                    OnPropertyChanged(nameof(HeightMm));
                    OnPropertyChanged(nameof(CanvasWidthDip));
                    OnPropertyChanged(nameof(CanvasHeightDip));
                    CheckDpi();
                }
            }
        }

        public bool IsCustomPageSize => _selectedPageSize?.Name == "Custom";

        public double WidthMm
        {
            get => _template.GetWidthMm();
            set
            {
                _customWidthMm = value;
                _template.CustomWidthMm = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanvasWidthDip));
                CheckDpi();
            }
        }

        public double HeightMm
        {
            get => _template.GetHeightMm();
            set
            {
                _customHeightMm = value;
                _template.CustomHeightMm = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanvasHeightDip));
                CheckDpi();
            }
        }

        public double CanvasWidthDip => WidthMm * FieldViewModel.MmToDip;
        public double CanvasHeightDip => HeightMm * FieldViewModel.MmToDip;

        public string? BackgroundPath
        {
            get => _backgroundPath;
            set
            {
                if (_backgroundPath != value)
                {
                    _backgroundPath = value;
                    _template.Background = value ?? string.Empty;
                    OnPropertyChanged();
                    CheckDpi();
                }
            }
        }

        public BitmapSource? BackgroundImageSource
        {
            get => _backgroundImageSource;
            set
            {
                _backgroundImageSource = value;
                OnPropertyChanged();
            }
        }

        public DpiCheckResult DpiInfo
        {
            get => _dpiInfo;
            set
            {
                _dpiInfo = value;
                OnPropertyChanged();
            }
        }

        public double Zoom
        {
            get => _zoom;
            set
            {
                _zoom = Math.Clamp(value, 0.2, 3.0);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomPercentage));
            }
        }

        public int ZoomPercentage => (int)Math.Round(Zoom * 100);

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public void SetBackground(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

            BackgroundPath = path;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                BackgroundImageSource = bmp;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal memuat preview gambar: {ex.Message}";
            }

            CheckDpi();
        }

        public void CheckDpi()
        {
            DpiInfo = CertificateRenderer.CheckBackgroundDpi(BackgroundPath, WidthMm, HeightMm);
        }

        public void LoadFieldsFromTemplate(CertificateTemplate template)
        {
            _template = template;

            // Set page size preset
            var preset = PageSizes.FirstOrDefault(p => string.Equals(p.Name, template.PageSize, StringComparison.OrdinalIgnoreCase))
                         ?? PageSizes[0];
            _selectedPageSize = preset;
            _customWidthMm = template.CustomWidthMm;
            _customHeightMm = template.CustomHeightMm;

            Fields.Clear();
            foreach (var f in template.Fields)
            {
                var fvm = new FieldViewModel(f);
                fvm.UpdatePreviewText(CurrentRecipient);
                Fields.Add(fvm);
            }

            if (Fields.Count > 0)
            {
                SelectedField = Fields[0];
            }

            if (!string.IsNullOrWhiteSpace(template.Background) && File.Exists(template.Background))
            {
                SetBackground(template.Background);
            }

            OnPropertyChanged(nameof(SelectedPageSize));
            OnPropertyChanged(nameof(WidthMm));
            OnPropertyChanged(nameof(HeightMm));
            OnPropertyChanged(nameof(CanvasWidthDip));
            OnPropertyChanged(nameof(CanvasHeightDip));
            CheckDpi();
        }

        public void RefreshFieldPreviews()
        {
            var cur = CurrentRecipient;
            foreach (var field in Fields)
            {
                field.UpdatePreviewText(cur);
            }
        }

        public void UpdateAvailableVariables()
        {
            var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Add variables from recipient data
            foreach (var rec in _recipients)
            {
                foreach (var k in rec.Data.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(k))
                    {
                        vars.Add(k.Trim());
                    }
                }
            }

            // 2. Add variables already defined in certificate template
            if (_template != null)
            {
                foreach (var v in _template.ExtractVariables())
                {
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        vars.Add(v.Trim());
                    }
                }
            }

            // 3. Fallback standard variables if none
            if (vars.Count == 0)
            {
                vars.Add("nama");
                vars.Add("nomor");
                vars.Add("peran");
                vars.Add("kegiatan");
                vars.Add("instansi");
                vars.Add("tanggal");
            }

            AvailableVariables.Clear();
            // Prioritize "nama" first
            if (vars.Contains("nama"))
            {
                AvailableVariables.Add("nama");
                vars.Remove("nama");
            }
            foreach (var v in vars.OrderBy(x => x))
            {
                AvailableVariables.Add(v);
            }
        }

        public FieldViewModel AddNewField(string? initialText = null)
        {
            SaveUndoState();

            int index = Fields.Count + 1;
            var def = new TextFieldDefinition
            {
                Id = $"teks_{index}",
                Text = initialText ?? "{nama}",
                FontFamily = "Arial",
                FontSize = 24.0,
                Color = "#1A1A1A",
                Align = "center",
                X = Math.Max(10, (WidthMm - 150) / 2),
                Y = Math.Min(HeightMm - 30, 60.0 + (index * 15)),
                BoxWidth = 150.0,
                AutoShrink = true
            };

            _template.Fields.Add(def);
            var fvm = new FieldViewModel(def);
            fvm.UpdatePreviewText(CurrentRecipient);
            Fields.Add(fvm);
            SelectedField = fvm;

            StatusMessage = $"Menambahkan kotak teks baru: {fvm.Id}";
            return fvm;
        }

        public void DeleteField(FieldViewModel? field)
        {
            if (field == null) field = SelectedField;
            if (field == null) return;

            SaveUndoState();
            DeleteFieldInternal(field);
            StatusMessage = $"Menghapus kotak teks: {field.Id}";
        }

        private void DeleteFieldInternal(FieldViewModel field)
        {
            int index = Fields.IndexOf(field);
            Fields.Remove(field);
            _template.Fields.Remove(field.Model);

            if (Fields.Count > 0)
            {
                SelectedField = Fields[Math.Clamp(index, 0, Fields.Count - 1)];
            }
            else
            {
                SelectedField = null;
            }
        }

        public void DuplicateField(FieldViewModel? field)
        {
            if (field == null) field = SelectedField;
            if (field == null) return;

            SaveUndoState();
            var cloneModel = field.Model.Clone();

            int counter = 1;
            string baseId = field.Id.Replace("_copy", "");
            string newId = $"{baseId}_copy";
            while (Fields.Any(f => f.Id.Equals(newId, StringComparison.OrdinalIgnoreCase)))
            {
                counter++;
                newId = $"{baseId}_copy{counter}";
            }
            cloneModel.Id = newId;

            cloneModel.X = Math.Min(WidthMm - 20, Math.Round(field.X + 8.0, 2));
            cloneModel.Y = Math.Min(HeightMm - 20, Math.Round(field.Y + 8.0, 2));

            _template.Fields.Add(cloneModel);
            var fvm = new FieldViewModel(cloneModel);
            fvm.UpdatePreviewText(CurrentRecipient);
            Fields.Add(fvm);
            SelectedField = fvm;

            StatusMessage = $"Diduplikasi: '{fvm.Id}' (Ctrl+D)";
        }

        #region Undo / Redo & Clipboard Management

        public class TemplateSnapshot
        {
            public List<TextFieldDefinition> Fields { get; set; } = new();
            public string? SelectedFieldId { get; set; }
        }

        private readonly Stack<TemplateSnapshot> _undoStack = new();
        private readonly Stack<TemplateSnapshot> _redoStack = new();
        private TextFieldDefinition? _clipboardField;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool HasClipboardField => _clipboardField != null;

        public void SaveUndoState()
        {
            _undoStack.Push(new TemplateSnapshot
            {
                Fields = Fields.Select(f => f.Model.Clone()).ToList(),
                SelectedFieldId = SelectedField?.Id
            });
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                StatusMessage = "Tidak ada aksi yang bisa di-undo.";
                return;
            }

            _redoStack.Push(new TemplateSnapshot
            {
                Fields = Fields.Select(f => f.Model.Clone()).ToList(),
                SelectedFieldId = SelectedField?.Id
            });

            var snapshot = _undoStack.Pop();
            RestoreSnapshot(snapshot);

            StatusMessage = "Undo: Perubahan terakhir dibatalkan (Ctrl+Z).";
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                StatusMessage = "Tidak ada aksi yang bisa di-redo.";
                return;
            }

            _undoStack.Push(new TemplateSnapshot
            {
                Fields = Fields.Select(f => f.Model.Clone()).ToList(),
                SelectedFieldId = SelectedField?.Id
            });

            var snapshot = _redoStack.Pop();
            RestoreSnapshot(snapshot);

            StatusMessage = "Redo: Perubahan diterapkan kembali (Ctrl+Y).";
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void RestoreSnapshot(TemplateSnapshot snapshot)
        {
            _template.Fields.Clear();
            Fields.Clear();

            foreach (var model in snapshot.Fields)
            {
                var clone = model.Clone();
                _template.Fields.Add(clone);
                var fvm = new FieldViewModel(clone);
                fvm.UpdatePreviewText(CurrentRecipient);
                Fields.Add(fvm);
            }

            if (!string.IsNullOrEmpty(snapshot.SelectedFieldId))
            {
                SelectedField = Fields.FirstOrDefault(f => f.Id == snapshot.SelectedFieldId) ?? Fields.FirstOrDefault();
            }
            else
            {
                SelectedField = Fields.FirstOrDefault();
            }
        }

        public void CopySelectedField()
        {
            if (SelectedField == null) return;
            _clipboardField = SelectedField.Model.Clone();
            StatusMessage = $"Disalin ke clipboard: '{SelectedField.Id}' (Ctrl+C). Tekan Ctrl+V untuk menempel.";
            OnPropertyChanged(nameof(HasClipboardField));
        }

        public void CutSelectedField()
        {
            if (SelectedField == null) return;
            SaveUndoState();
            _clipboardField = SelectedField.Model.Clone();
            string cutId = SelectedField.Id;
            DeleteFieldInternal(SelectedField);
            StatusMessage = $"Dipotong: '{cutId}' (Ctrl+X). Tekan Ctrl+V untuk menempel.";
            OnPropertyChanged(nameof(HasClipboardField));
        }

        public void PasteField()
        {
            if (_clipboardField == null)
            {
                StatusMessage = "Clipboard kosong. Salin kotak teks terlebih dahulu dengan Ctrl+C.";
                return;
            }

            SaveUndoState();
            var clone = _clipboardField.Clone();

            int counter = 1;
            string baseId = clone.Id.Replace("_copy", "");
            string newId = $"{baseId}_copy";
            while (Fields.Any(f => f.Id.Equals(newId, StringComparison.OrdinalIgnoreCase)))
            {
                counter++;
                newId = $"{baseId}_copy{counter}";
            }
            clone.Id = newId;

            // Offset slightly so it doesn't completely overlap
            clone.X = Math.Min(WidthMm - 20, Math.Round(clone.X + 8.0, 2));
            clone.Y = Math.Min(HeightMm - 20, Math.Round(clone.Y + 8.0, 2));

            _template.Fields.Add(clone);
            var fvm = new FieldViewModel(clone);
            fvm.UpdatePreviewText(CurrentRecipient);
            Fields.Add(fvm);
            SelectedField = fvm;

            StatusMessage = $"Ditempel: '{clone.Id}' di X={clone.X:F1}mm, Y={clone.Y:F1}mm (Ctrl+V).";
        }

        #endregion

        private bool _snapToCenterEnabled = true;

        public bool SnapToCenterEnabled
        {
            get => _snapToCenterEnabled;
            set
            {
                if (_snapToCenterEnabled != value)
                {
                    _snapToCenterEnabled = value;
                    OnPropertyChanged();
                    StatusMessage = value ? "Pemandu garis tengah (Snap to Center) aktif." : "Pemandu garis tengah (Snap to Center) dimatikan.";
                }
            }
        }

        public void CenterFieldHorizontally(FieldViewModel? field)
        {
            if (field == null) field = SelectedField;
            if (field == null) return;

            SaveUndoState();
            double newX = Math.Round(Math.Max(0, (WidthMm - field.BoxWidth) / 2.0), 2);
            field.X = newX;
            StatusMessage = $"Kotak teks '{field.Id}' ditengahkan secara horizontal ke posisi X={newX:F1} mm.";
        }

        public void CenterFieldVertically(FieldViewModel? field)
        {
            if (field == null) field = SelectedField;
            if (field == null) return;

            SaveUndoState();
            // Approximate center vertically
            double newY = Math.Round(Math.Max(0, (HeightMm - 20) / 2.0), 2);
            field.Y = newY;
            StatusMessage = $"Kotak teks '{field.Id}' ditengahkan secara vertikal ke posisi Y={newY:F1} mm.";
        }

        public void NudgeField(FieldViewModel? field, double deltaXMm, double deltaYMm)
        {
            if (field == null) field = SelectedField;
            if (field == null) return;

            SaveUndoState();
            double newX = Math.Clamp(Math.Round(field.X + deltaXMm, 2), 0, Math.Max(0, WidthMm - 10));
            double newY = Math.Clamp(Math.Round(field.Y + deltaYMm, 2), 0, Math.Max(0, HeightMm - 10));

            field.X = newX;
            field.Y = newY;
            StatusMessage = $"Geser '{field.Id}': X = {newX:F1} mm, Y = {newY:F1} mm";
        }

        public void NextRecipient()
        {
            if (CurrentRecipientIndex < TotalRecipients - 1)
            {
                CurrentRecipientIndex++;
            }
        }

        public void PrevRecipient()
        {
            if (CurrentRecipientIndex > 0)
            {
                CurrentRecipientIndex--;
            }
        }

        public void FirstRecipient()
        {
            if (TotalRecipients > 0)
            {
                CurrentRecipientIndex = 0;
            }
        }

        public void LastRecipient()
        {
            if (TotalRecipients > 0)
            {
                CurrentRecipientIndex = TotalRecipients - 1;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
