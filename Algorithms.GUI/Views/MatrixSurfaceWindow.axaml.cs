using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.Core.MatrixAlgorithms;
using Algorithms.GUI.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Algorithms.GUI.Models
{
    public class MatrixSeries
    {
        public string Name { get; set; } = string.Empty;
        public List<MatrixBenchmarkResult> Results { get; set; } = new();
        public Color Color { get; set; } = Colors.Teal;
    }

    public class SeriesLegendItem
    {
        public string Name { get; set; } = string.Empty;
        public IBrush Brush { get; set; } = Brushes.Teal;

        // Новые свойства для видимости:
        public bool IsVisible { get; set; } = true;
        public Action<bool>? OnVisibilityChanged { get; set; }
    }
}

namespace Algorithms.GUI.Views
{
    public partial class MatrixSurfaceControl : UserControl
    {
        private const double DefaultRotation = 270;
        private const double DefaultPanOffsetY = -80;

        private class InternalSeries
        {
            public string Name { get; set; } = string.Empty;
            public double[,] Grid { get; set; } = new double[0, 0];
            public Color Color { get; set; } = Colors.Teal;

            public bool IsVisible { get; set; } = true; // Добавлено
        }

        private int[] _nValues = Array.Empty<int>();
        private int[] _mValues = Array.Empty<int>();
        private List<InternalSeries> _seriesList = new();
        private bool _useHeatmap = true;

        private double _maxTime;
        private double _pivotHeight;

        private double _rotationAngle = DefaultRotation;
        private double _zoom = 1.0;
        private double _defaultZoom = 1.0;
        private double _panOffsetX;
        private double _panOffsetY;

        private bool _isPanning;
        private bool _isRotating;
        private Point _lastPointerPosition;
        private bool _hasResults;

        public MatrixSurfaceControl()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                if (_hasResults)
                {
                    RecomputeDefaultZoomAndReset();
                }
            };
        }

        public void SetResults(List<MatrixBenchmarkResult> results)
        {
            if (results == null || results.Count == 0) return;

            _seriesList.Clear();
            _nValues = results.Select(r => r.N).Distinct().OrderBy(x => x).ToArray();
            _mValues = results.Select(r => r.M).Distinct().OrderBy(x => x).ToArray();
            _maxTime = results.Max(r => r.TimeMs);

            var grid = new double[_nValues.Length, _mValues.Length];

            // Инициализируем пустоту как NaN, а не 0.0
            for (int i = 0; i < _nValues.Length; i++)
            for (int j = 0; j < _mValues.Length; j++)
                grid[i, j] = double.NaN;

            foreach (var r in results)
            {
                int i = Array.IndexOf(_nValues, r.N);
                int j = Array.IndexOf(_mValues, r.M);
                if (i >= 0 && j >= 0) grid[i, j] = r.TimeMs;
            }

            _seriesList = new List<InternalSeries>
            {
                new InternalSeries
                {
                    Name = "Умножение матриц",
                    Grid = grid,
                    Color = Colors.Teal
                }
            };

            _maxTime = results.Max(r => r.TimeMs);
            if (_maxTime <= 0) _maxTime = 1.0;
            _pivotHeight = _maxTime / 2.0;
            _hasResults = true;

            HeatmapLegendPanel.IsVisible = true;
            SeriesLegendPanel.IsVisible = false;
            LegendMaxText.Text = $"{_maxTime:F2}";
            LegendMinText.Text = "0";

            RecomputeDefaultZoomAndReset();
        }

        private void LegendEyeButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn && btn.DataContext is SeriesLegendItem item)
            {
                bool isVisible = btn.IsChecked ?? false;
                btn.Opacity = isVisible ? 1.0 : 0.4;
                item.IsVisible = isVisible;
                item.OnVisibilityChanged?.Invoke(isVisible);
            }
        }

        public void SetMultipleResults(List<MatrixSeries> seriesList)
        {
            if (seriesList == null || seriesList.Count == 0) return;

            _useHeatmap = false;
            _nValues = seriesList.SelectMany(s => s.Results.Select(r => r.N)).Distinct().OrderBy(x => x).ToArray();
            _mValues = seriesList.SelectMany(s => s.Results.Select(r => r.M)).Distinct().OrderBy(x => x).ToArray();

            _seriesList = new List<InternalSeries>();
            double maxTime = 0;

            var legendItems = new List<SeriesLegendItem>();

            foreach (var s in seriesList)
            {
                var grid = new double[_nValues.Length, _mValues.Length];

                // Инициализируем пустоту как NaN
                for (int i = 0; i < _nValues.Length; i++)
                for (int j = 0; j < _mValues.Length; j++)
                    grid[i, j] = double.NaN;

                foreach (var r in s.Results)
                {
                    int i = Array.IndexOf(_nValues, r.N);
                    int j = Array.IndexOf(_mValues, r.M);
                    if (i >= 0 && j >= 0)
                    {
                        grid[i, j] = r.TimeMs;
                        if (r.TimeMs > maxTime) maxTime = r.TimeMs;
                    }
                }

                var internalSeries = new InternalSeries
                {
                    Name = s.Name,
                    Grid = grid,
                    Color = s.Color,
                    IsVisible = true
                };
                _seriesList.Add(internalSeries);

                legendItems.Add(new SeriesLegendItem
                {
                    Name = s.Name,
                    Brush = new SolidColorBrush(s.Color),
                    IsVisible = true,
                    OnVisibilityChanged = (isVisible) =>
                    {
                        internalSeries.IsVisible = isVisible;
                        DrawSurface(); // Перерисовываем при нажатии на глазик
                    }
                });
            }

            _maxTime = maxTime <= 0 ? 1.0 : maxTime;
            _pivotHeight = _maxTime / 2.0;
            _hasResults = true;

            HeatmapLegendPanel.IsVisible = false;
            SeriesLegendPanel.IsVisible = true;
            SeriesLegendItems.ItemsSource = legendItems;

            RecomputeDefaultZoomAndReset();
        }

        private void RecomputeDefaultZoomAndReset()
        {
            double canvasW = DrawCanvas.Bounds.Width > 0 ? DrawCanvas.Bounds.Width : 420;
            double canvasH = DrawCanvas.Bounds.Height > 0 ? DrawCanvas.Bounds.Height : 420;
            _defaultZoom = ComputeAutoFitZoom(canvasW, canvasH);
            ResetView();
        }

        /// <summary>
        /// Сбрасывает поворот, масштаб и сдвиг к значениям по умолчанию.
        /// </summary>
        public void ResetView()
        {
            _rotationAngle = DefaultRotation;
            _zoom = _defaultZoom;
            _panOffsetX = 0;
            _panOffsetY = DefaultPanOffsetY;
            DrawSurface();
        }

        /// <summary>
        /// Меняет масштаб поверхности на заданный множитель (например, 1.2 — приблизить).
        /// </summary>
        public void ZoomBy(double factor)
        {
            _zoom = Math.Clamp(_zoom * factor, 0.2, 5.0);
            DrawSurface();
        }

        private double ComputeAutoFitZoom(double canvasW, double canvasH)
        {
            int rows = _nValues.Length, cols = _mValues.Length;
            if (rows < 2 || cols < 2 || _seriesList.Count == 0) return 1.0;

            double cx = (rows - 1) / 2.0, cz = (cols - 1) / 2.0;
            double angle = DefaultRotation * Math.PI / 180.0;
            double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
            double scale = 6.0;
            double heightScale = _maxTime > 0 ? 150.0 / _maxTime : 1.0;

            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            bool hasData = false;

            foreach (var series in _seriesList)
            {
                if (!series.IsVisible) continue;
                
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        if (double.IsNaN(series.Grid[i, j])) continue; // Игнорируем непосчитанные

                        hasData = true;
                        double dx = i - cx;
                        double dz = j - cz;
                        double rx = dx * cosA - dz * sinA;
                        double rz = dx * sinA + dz * cosA;

                        double sx = (rx - rz) * scale * Math.Cos(Math.PI / 6);
                        double sy = (rx + rz) * scale * Math.Sin(Math.PI / 6) -
                                    (series.Grid[i, j] - _pivotHeight) * heightScale;

                        if (sx < minX) minX = sx;
                        if (sx > maxX) maxX = sx;
                        if (sy < minY) minY = sy;
                        if (sy > maxY) maxY = sy;
                    }
                }
            }

            if (!hasData) return 1.0;

            double bboxWidth = Math.Max(maxX - minX, 1);
            double bboxHeight = Math.Max(maxY - minY, 1);

            double fitX = canvasW * 0.7 / bboxWidth;
            double fitY = canvasH * 0.7 / bboxHeight;

            return Math.Clamp(Math.Min(fitX, fitY), 0.2, 5.0);
        }

        private void DrawCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            double zoomFactor = e.Delta.Y > 0 ? 1.1 : 0.9;
            _zoom = Math.Clamp(_zoom * zoomFactor, 0.2, 5.0);
            DrawSurface();
            e.Handled = true;
        }

        private void DrawCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(DrawCanvas);
            _isRotating = point.Properties.IsRightButtonPressed;
            _isPanning = point.Properties.IsLeftButtonPressed;
            _lastPointerPosition = e.GetPosition(DrawCanvas);
            e.Pointer.Capture(DrawCanvas);
        }

        private void DrawCanvas_PointerMoved(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(DrawCanvas);

            if (_isRotating)
            {
                double deltaX = pos.X - _lastPointerPosition.X;
                _rotationAngle = (_rotationAngle + deltaX * 0.5) % 360;
                if (_rotationAngle < 0) _rotationAngle += 360;
                DrawSurface();
            }
            else if (_isPanning)
            {
                _panOffsetX += pos.X - _lastPointerPosition.X;
                _panOffsetY += pos.Y - _lastPointerPosition.Y;
                DrawSurface();
            }

            _lastPointerPosition = pos;
        }

        private void DrawCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isPanning = false;
            _isRotating = false;
            e.Pointer.Capture(null);
        }

        private void DrawSurface()
        {
            DrawCanvas.Children.Clear();
            if (!_hasResults || _seriesList.Count == 0) return;

            int rows = _nValues.Length;
            int cols = _mValues.Length;
            if (rows == 0 || cols == 0) return; // Теперь график отрисуется, даже если успела посчитаться 1 точка

            double angle = _rotationAngle * Math.PI / 180.0;
            double cosA = Math.Cos(angle);
            double sinA = Math.Sin(angle);

            double cx = (rows - 1) / 2.0;
            double cz = (cols - 1) / 2.0;

            double canvasW = DrawCanvas.Bounds.Width > 0 ? DrawCanvas.Bounds.Width : 420;
            double canvasH = DrawCanvas.Bounds.Height > 0 ? DrawCanvas.Bounds.Height : 420;

            double scale = 6.0 * _zoom;
            double heightScale = (_maxTime > 0 ? 150.0 / _maxTime : 1.0) * _zoom;

            double offsetX = canvasW / 2 + _panOffsetX;
            double offsetY = canvasH / 2 + _panOffsetY;

            (double sx, double sy) Project(double x, double z, double y)
            {
                double dx = x - cx;
                double dz = z - cz;

                double rx = dx * cosA - dz * sinA;
                double rz = dx * sinA + dz * cosA;

                double screenX = (rx - rz) * scale * Math.Cos(Math.PI / 6) + offsetX;
                double screenY = (rx + rz) * scale * Math.Sin(Math.PI / 6) - (y - _pivotHeight) * heightScale + offsetY;
                return (screenX, screenY);
            }

            int rEnd = Math.Max(0, rows - 1);
            int cEnd = Math.Max(0, cols - 1);
            var floorEdges = new (int i0, int j0, int i1, int j1)[]
            {
                (0, 0, rEnd, 0),
                (0, 0, 0, cEnd),
                (rEnd, 0, rEnd, cEnd),
                (0, cEnd, rEnd, cEnd)
            };
            foreach (var (i0, j0, i1, j1) in floorEdges)
            {
                var a = Project(i0, j0, 0);
                var b = Project(i1, j1, 0);
                DrawCanvas.Children.Add(new Line
                {
                    StartPoint = new Point(a.sx, a.sy),
                    EndPoint = new Point(b.sx, b.sy),
                    Stroke = new SolidColorBrush(Color.FromArgb(120, 100, 100, 100)),
                    StrokeThickness = 1
                });
            }

            AddAxisLabel(Project(0, 0, 0), $"n={_nValues[0]}, m={_mValues[0]}");
            AddAxisLabel(Project(rEnd, 0, 0), $"n={_nValues[^1]}");
            AddAxisLabel(Project(0, cEnd, 0), $"m={_mValues[^1]}");

            // Вертикальная шкала времени (мс) в дальнем углу основания
            DrawTimeAxis(Project, rEnd, cEnd);

            var elementsToDraw = new List<(Control Element, double Depth)>();

            foreach (var series in _seriesList)
            {
                if (!series.IsVisible) continue;
                
                Color fillColor, strokeColor;
                if (_useHeatmap)
                {
                    fillColor = Colors.White;
                    strokeColor = Color.FromArgb(80, 0, 0, 0);
                }
                else
                {
                    fillColor = Color.FromArgb(130, series.Color.R, series.Color.G, series.Color.B);
                    strokeColor = Color.FromArgb(180, series.Color.R, series.Color.G, series.Color.B);
                }

                // 1. Отрисовка поверхностей (полигонов)
                if (rows > 1 && cols > 1)
                {
                    for (int i = 0; i < rows - 1; i++)
                    {
                        for (int j = 0; j < cols - 1; j++)
                        {
                            double h00 = series.Grid[i, j];
                            double h10 = series.Grid[i + 1, j];
                            double h01 = series.Grid[i, j + 1];
                            double h11 = series.Grid[i + 1, j + 1];

                            // Пропускаем квадрат, если хотя бы один из его 4 углов не был вычислен (отменен)
                            if (double.IsNaN(h00) || double.IsNaN(h10) || double.IsNaN(h01) || double.IsNaN(h11))
                                continue;

                            var p00 = Project(i, j, h00);
                            var p10 = Project(i + 1, j, h10);
                            var p01 = Project(i, j + 1, h01);
                            var p11 = Project(i + 1, j + 1, h11);

                            double avgHeight = (h00 + h10 + h01 + h11) / 4.0;
                            IBrush polyFill = _useHeatmap
                                ? new SolidColorBrush(HeightToColor(_maxTime > 0 ? avgHeight / _maxTime : 0))
                                : new SolidColorBrush(fillColor);

                            var poly = new Polygon
                            {
                                Points = new Points
                                {
                                    new(p00.sx, p00.sy), new(p10.sx, p10.sy), new(p11.sx, p11.sy), new(p01.sx, p01.sy)
                                },
                                Fill = polyFill,
                                Stroke = new SolidColorBrush(strokeColor),
                                StrokeThickness = _useHeatmap ? 0.5 : 0.8
                            };

                            double dx = (i + 0.5) - cx;
                            double dz = (j + 0.5) - cz;
                            double depth = ((dx * cosA - dz * sinA) + (dx * sinA + dz * cosA)) * 10000.0 + avgHeight;
                            elementsToDraw.Add((poly, depth));
                        }
                    }
                }

                // 2. Отрисовка точек-узлов (позволит увидеть данные, даже если нет полных полигонов)
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        double h = series.Grid[i, j];
                        if (double.IsNaN(h)) continue;

                        var p = Project(i, j, h);

                        double dx = i - cx;
                        double dz = j - cz;
                        double depth = ((dx * cosA - dz * sinA) + (dx * sinA + dz * cosA)) * 10000.0 + h + 1.0;

                        var dotColor = _useHeatmap ? HeightToColor(_maxTime > 0 ? h / _maxTime : 0) : series.Color;
                        var dot = new Ellipse
                        {
                            Width = 4, Height = 4,
                            Fill = new SolidColorBrush(dotColor),
                            IsHitTestVisible = false
                        };
                        Canvas.SetLeft(dot, p.sx - 2);
                        Canvas.SetTop(dot, p.sy - 2);

                        elementsToDraw.Add((dot, depth));
                    }
                }
            }

            // Рендер отсортированный по глубине Z-index
            foreach (var (element, _) in elementsToDraw.OrderBy(e => e.Depth))
            {
                DrawCanvas.Children.Add(element);
            }
        }

        private void DrawTimeAxis(Func<double, double, double, (double sx, double sy)> project, int rEnd, int cEnd)
        {
            if (_maxTime <= 0) return;

            const int tickCount = 4;
            var axisBase = project(rEnd, cEnd, 0);
            var axisTop = project(rEnd, cEnd, _maxTime);

            var axisLine = new Line
            {
                StartPoint = new Point(axisBase.sx, axisBase.sy),
                EndPoint = new Point(axisTop.sx, axisTop.sy),
                Stroke = new SolidColorBrush(Color.FromRgb(0x37, 0x94, 0xFF)),
                StrokeThickness = 1.4
            };
            DrawCanvas.Children.Add(axisLine);

            for (int t = 0; t <= tickCount; t++)
            {
                double value = _maxTime * t / tickCount;
                var p = project(rEnd, cEnd, value);

                var tick = new Line
                {
                    StartPoint = new Point(p.sx - 5, p.sy),
                    EndPoint = new Point(p.sx + 5, p.sy),
                    Stroke = new SolidColorBrush(Color.FromRgb(0xB4, 0xB4, 0xB4)),
                    StrokeThickness = 1
                };
                DrawCanvas.Children.Add(tick);

                string valueText = value switch
                {
                    < 1 => $"{value:F2}",
                    < 10 => $"{value:F1}",
                    _ => $"{value:F0}"
                };

                var label = new TextBlock
                {
                    Text = $"{valueText} мс",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4))
                };
                Canvas.SetLeft(label, p.sx + 7);
                Canvas.SetTop(label, p.sy - 7);
                DrawCanvas.Children.Add(label);
            }

            var axisTitle = new TextBlock
            {
                Text = "Время (мс)",
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4))
            };
            Canvas.SetLeft(axisTitle, axisTop.sx + 7);
            Canvas.SetTop(axisTitle, axisTop.sy - 22);
            DrawCanvas.Children.Add(axisTitle);
        }

        private void AddAxisLabel((double sx, double sy) pos, string text)
        {
            var label = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontSize = 10
            };
            Canvas.SetLeft(label, pos.sx);
            Canvas.SetTop(label, pos.sy);
            DrawCanvas.Children.Add(label);
        }

        private static Color HeightToColor(double t)
        {
            t = Math.Clamp(t, 0, 1);
            if (t < 0.5)
            {
                double k = t / 0.5;
                return Color.FromRgb(0, (byte)(k * 255), (byte)((1 - k) * 255));
            }

            double k2 = (t - 0.5) / 0.5;
            return Color.FromRgb((byte)(k2 * 255), (byte)((1 - k2) * 255), 0);
        }
    }
}