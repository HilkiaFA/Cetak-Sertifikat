using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CetakSertifikatApp.ViewModels;

namespace CetakSertifikatApp.Controls
{
    public partial class CertificateCanvas : UserControl
    {
        private bool _isDragging = false;
        private bool _isResizing = false;
        private bool _hasSavedUndoForDrag = false;
        private Point _startMousePos;
        private double _initialLeft;
        private double _initialTop;
        private double _initialWidth;
        private FieldViewModel? _activeField;

        public CertificateCanvas()
        {
            InitializeComponent();
            FieldCanvas.LostMouseCapture += FieldCanvas_LostMouseCapture;
        }

        private void FieldCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            ResetDragState();
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void FieldCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var hit = e.OriginalSource as DependencyObject;
            var fieldVm = FindParentDataContext<FieldViewModel>(hit);

            if (fieldVm != null && ViewModel != null)
            {
                ViewModel.SelectedField = fieldVm;
                _activeField = fieldVm;
                _startMousePos = e.GetPosition(FieldCanvas);
                _initialLeft = fieldVm.CanvasLeft;
                _initialTop = fieldVm.CanvasTop;
                _initialWidth = fieldVm.CanvasWidth;
                _hasSavedUndoForDrag = false;

                // Check if user clicked on the resize grip
                var border = FindParentWithTag(hit, "ResizeGrip");
                if (border != null)
                {
                    _isResizing = true;
                    _isDragging = false;
                }
                else
                {
                    _isDragging = true;
                    _isResizing = false;
                }

                FieldCanvas.CaptureMouse();
                FieldCanvas.Focus();
                e.Handled = true;
            }
            else
            {
                // Clicked on blank canvas
                if (ViewModel != null)
                {
                    ViewModel.SelectedField = null;
                }
            }
        }

        private void FieldCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_activeField == null || (!_isDragging && !_isResizing)) return;

            Point curPos = e.GetPosition(FieldCanvas);
            double deltaX = curPos.X - _startMousePos.X;
            double deltaY = curPos.Y - _startMousePos.Y;

            if ((_isDragging || _isResizing) && !_hasSavedUndoForDrag && (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1))
            {
                ViewModel?.SaveUndoState();
                _hasSavedUndoForDrag = true;
            }

            double maxW = ViewModel != null ? ViewModel.CanvasWidthDip : 2000;
            double maxH = ViewModel != null ? ViewModel.CanvasHeightDip : 2000;

            if (_isDragging)
            {
                double currentWidth = _activeField.CanvasWidth;
                double candidateLeft = Math.Clamp(_initialLeft + deltaX, 0, Math.Max(0, maxW - 20));
                double candidateTop = Math.Clamp(_initialTop + deltaY, 0, Math.Max(0, maxH - 10));

                bool isSnapActive = ViewModel?.SnapToCenterEnabled ?? true;
                bool snappedX = false;
                bool snappedY = false;
                string snapMessage = string.Empty;
                double snapGuideX = 0;
                double snapGuideY = 0;

                if (isSnapActive)
                {
                    const double SnapThreshold = 8.0; // DIP threshold
                    double candidateCenterX = candidateLeft + (currentWidth / 2.0);

                    // 1. Check Snap to Canvas Center X (Garis Tengah Halaman Vertikal)
                    double canvasCenterX = maxW / 2.0;
                    if (Math.Abs(candidateCenterX - canvasCenterX) <= SnapThreshold)
                    {
                        candidateLeft = canvasCenterX - (currentWidth / 2.0);
                        snappedX = true;
                        snapGuideX = canvasCenterX;
                        double mmX = canvasCenterX / FieldViewModel.MmToDip;
                        snapMessage = $"Pusat Halaman (X: {mmX:F1} mm)";
                    }
                    else if (ViewModel != null)
                    {
                        // 2. Check Snap to other fields' Center X (Sejajar Tengah dengan item lain)
                        foreach (var other in ViewModel.Fields)
                        {
                            if (other == _activeField) continue;
                            double otherCenterX = other.CanvasLeft + (other.CanvasWidth / 2.0);
                            if (Math.Abs(candidateCenterX - otherCenterX) <= SnapThreshold)
                            {
                                candidateLeft = otherCenterX - (currentWidth / 2.0);
                                snappedX = true;
                                snapGuideX = otherCenterX;
                                snapMessage = $"Sejajar Tengah dengan '{other.Id}'";
                                break;
                            }
                        }
                    }

                    // 3. Check Snap to Canvas Center Y (Garis Tengah Halaman Horizontal)
                    double canvasCenterY = maxH / 2.0;
                    double candidateCenterY = candidateTop + 15.0; // Perkiraan titik tengah tinggi teks
                    if (Math.Abs(candidateCenterY - canvasCenterY) <= SnapThreshold)
                    {
                        candidateTop = canvasCenterY - 15.0;
                        snappedY = true;
                        snapGuideY = canvasCenterY;
                        if (string.IsNullOrEmpty(snapMessage))
                        {
                            snapMessage = "Tengah Vertikal Halaman";
                        }
                    }
                }

                _activeField.CanvasLeft = candidateLeft;
                _activeField.CanvasTop = candidateTop;

                // Update Guide Lines Visibility
                if (snappedX)
                {
                    VerticalGuideLine.X1 = snapGuideX;
                    VerticalGuideLine.X2 = snapGuideX;
                    VerticalGuideLine.Y1 = 0;
                    VerticalGuideLine.Y2 = maxH;
                    VerticalGuideLine.Visibility = Visibility.Visible;

                    GuideBadge.Visibility = Visibility.Visible;
                    GuideBadgeText.Text = snapMessage;
                    Canvas.SetLeft(GuideBadge, Math.Clamp(snapGuideX - 70, 10, maxW - 170));
                    Canvas.SetTop(GuideBadge, 8);
                }
                else
                {
                    VerticalGuideLine.Visibility = Visibility.Collapsed;
                    if (!snappedY)
                    {
                        GuideBadge.Visibility = Visibility.Collapsed;
                    }
                }

                if (snappedY)
                {
                    HorizontalGuideLine.X1 = 0;
                    HorizontalGuideLine.X2 = maxW;
                    HorizontalGuideLine.Y1 = snapGuideY;
                    HorizontalGuideLine.Y2 = snapGuideY;
                    HorizontalGuideLine.Visibility = Visibility.Visible;
                }
                else
                {
                    HorizontalGuideLine.Visibility = Visibility.Collapsed;
                }
            }
            else if (_isResizing)
            {
                double newW = Math.Max(30, _initialWidth + deltaX);
                
                // Snap resize width so center point matches canvas center
                if (ViewModel?.SnapToCenterEnabled ?? true)
                {
                    double canvasCenterX = maxW / 2.0;
                    double curCenterX = _activeField.CanvasLeft + (newW / 2.0);
                    if (Math.Abs(curCenterX - canvasCenterX) <= 8.0)
                    {
                        double snappedW = (canvasCenterX - _activeField.CanvasLeft) * 2.0;
                        if (snappedW >= 30)
                        {
                            newW = snappedW;
                            VerticalGuideLine.X1 = canvasCenterX;
                            VerticalGuideLine.X2 = canvasCenterX;
                            VerticalGuideLine.Y1 = 0;
                            VerticalGuideLine.Y2 = maxH;
                            VerticalGuideLine.Visibility = Visibility.Visible;

                            GuideBadge.Visibility = Visibility.Visible;
                            GuideBadgeText.Text = "Pusat Halaman (Lebar Simetris)";
                            Canvas.SetLeft(GuideBadge, Math.Clamp(canvasCenterX - 70, 10, maxW - 170));
                            Canvas.SetTop(GuideBadge, 8);
                        }
                    }
                    else
                    {
                        VerticalGuideLine.Visibility = Visibility.Collapsed;
                        GuideBadge.Visibility = Visibility.Collapsed;
                    }
                }

                _activeField.CanvasWidth = newW;
            }
        }

        private void FieldCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging || _isResizing)
            {
                FieldCanvas.ReleaseMouseCapture();
                ResetDragState();
            }
        }

        private void ResetDragState()
        {
            _isDragging = false;
            _isResizing = false;
            _hasSavedUndoForDrag = false;
            _activeField = null;

            // Sembunyikan garis pandu setelah mouse dilepas
            if (VerticalGuideLine != null) VerticalGuideLine.Visibility = Visibility.Collapsed;
            if (HorizontalGuideLine != null) HorizontalGuideLine.Visibility = Visibility.Collapsed;
            if (GuideBadge != null) GuideBadge.Visibility = Visibility.Collapsed;
        }

        private static T? FindParentDataContext<T>(DependencyObject? child) where T : class
        {
            var cur = child;
            while (cur != null)
            {
                if (cur is FrameworkElement fe && fe.DataContext is T target)
                {
                    return target;
                }
                cur = VisualTreeHelper.GetParent(cur);
            }
            return null;
        }

        private static FrameworkElement? FindParentWithTag(DependencyObject? child, string tag)
        {
            var cur = child;
            while (cur != null)
            {
                if (cur is FrameworkElement fe && string.Equals(fe.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return fe;
                }
                cur = VisualTreeHelper.GetParent(cur);
            }
            return null;
        }
    }
}
