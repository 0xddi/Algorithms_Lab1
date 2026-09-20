using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.Core.MatrixAlgorithms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Algorithms.GUI.Views;

public partial class MatrixSurfaceControl : UserControl
{
    private const double DefaultRotation = 270;
    private const double DefaultPanOffsetY = -80;

    private int[] _nValues = Array.Empty<int>();
    private int[] _mValues = Array.Empty<int>();
    private double[,] _grid = new double[0, 0];
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
        _nValues = results.Select(r => r.N).Distinct().OrderBy(x => x).ToArray();
        _mValues = results.Select(r => r.M).Distinct().OrderBy(x => x).ToArray();

        _grid = new double[_nValues.Length, _mValues.Length];
        foreach (var r in results)
        {
            int i = Array.IndexOf(_nValues, r.N);
            int j = Array.IndexOf(_mValues, r.M);
            _grid[i, j] = r.TimeMs;
        }

        _maxTime = results.Max(r => r.TimeMs);
        _pivotHeight = _maxTime / 2.0;
        _hasResults = true;

        LegendMaxText.Text = $"{_maxTime:F2}";
        LegendMinText.Text = "0";

        RecomputeDefaultZoomAndReset();
    }

    private void RecomputeDefaultZoomAndReset()
    {
        double canvasW = DrawCanvas.Bounds.Width > 0 ? DrawCanvas.Bounds.Width : 420;
        double canvasH = DrawCanvas.Bounds.Height > 0 ? DrawCanvas.Bounds.Height : 420;
        _defaultZoom = ComputeAutoFitZoom(canvasW, canvasH);
        ResetView();
    }

    private void ResetViewButton_Click(object? sender, RoutedEventArgs e) => ResetView();

    private void ResetView()
    {
        _rotationAngle = DefaultRotation;
        _zoom = _defaultZoom;
        _panOffsetX = 0;
        _panOffsetY = DefaultPanOffsetY;

        if (Math.Abs(RotationSlider.Value - _rotationAngle) > 0.01)
            RotationSlider.Value = _rotationAngle;
        else
            DrawSurface();
    }

    private double ComputeAutoFitZoom(double canvasW, double canvasH)
    {
        int rows = _nValues.Length, cols = _mValues.Length;
        if (rows < 2 || cols < 2) return 1.0;

        double cx = (rows - 1) / 2.0, cz = (cols - 1) / 2.0;
        double angle = DefaultRotation * Math.PI / 180.0;
        double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
        double scale = 6.0;
        double heightScale = _maxTime > 0 ? 150.0 / _maxTime : 1.0;

        double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                double dx = i - cx, dz = j - cz;
                double rx = dx * cosA - dz * sinA;
                double rz = dx * sinA + dz * cosA;
                double sx = (rx - rz) * scale * Math.Cos(Math.PI / 6);
                double sy = (rx + rz) * scale * Math.Sin(Math.PI / 6) - (_grid[i, j] - _pivotHeight) * heightScale;

                if (sx < minX) minX = sx;
                if (sx > maxX) maxX = sx;
                if (sy < minY) minY = sy;
                if (sy > maxY) maxY = sy;
            }
        }

        double bboxWidth = Math.Max(maxX - minX, 1);
        double bboxHeight = Math.Max(maxY - minY, 1);

        double fitX = canvasW * 0.7 / bboxWidth;
        double fitY = canvasH * 0.7 / bboxHeight;

        return Math.Clamp(Math.Min(fitX, fitY), 0.2, 5.0);
    }

    private void RotationSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _rotationAngle = e.NewValue;
        DrawSurface();
    }

    private void DrawCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        double zoomFactor = e.Delta.Y > 0 ? 1.1 : 0.9;
        _zoom = Math.Clamp(_zoom * zoomFactor, 0.2, 5.0);
        DrawSurface();
        e.Handled = true; // не отдаём колесо внешнему ScrollViewer с графиками
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
            RotationSlider.Value = _rotationAngle;
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
        if (!_hasResults) return;

        int rows = _nValues.Length;
        int cols = _mValues.Length;
        if (rows < 2 || cols < 2) return;

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

        var floorEdges = new (int i0, int j0, int i1, int j1)[]
        {
            (0, 0, rows - 1, 0),
            (0, 0, 0, cols - 1),
            (rows - 1, 0, rows - 1, cols - 1),
            (0, cols - 1, rows - 1, cols - 1)
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
        AddAxisLabel(Project(rows - 1, 0, 0), $"n={_nValues[^1]}");
        AddAxisLabel(Project(0, cols - 1, 0), $"m={_mValues[^1]}");

        var quads = new List<(Polygon poly, double depth)>();

        for (int i = 0; i < rows - 1; i++)
        {
            for (int j = 0; j < cols - 1; j++)
            {
                double h00 = _grid[i, j];
                double h10 = _grid[i + 1, j];
                double h01 = _grid[i, j + 1];
                double h11 = _grid[i + 1, j + 1];

                var p00 = Project(i, j, h00);
                var p10 = Project(i + 1, j, h10);
                var p01 = Project(i, j + 1, h01);
                var p11 = Project(i + 1, j + 1, h11);

                var poly = new Polygon
                {
                    Points = new Points { new(p00.sx, p00.sy), new(p10.sx, p10.sy), new(p11.sx, p11.sy), new(p01.sx, p01.sy) },
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                    StrokeThickness = 0.5
                };

                double avgHeight = (h00 + h10 + h01 + h11) / 4.0;
                double t = _maxTime > 0 ? avgHeight / _maxTime : 0;
                poly.Fill = new SolidColorBrush(HeightToColor(t));

                quads.Add((poly, i + j));
            }
        }

        foreach (var (poly, _) in quads.OrderBy(q => q.depth))
        {
            DrawCanvas.Children.Add(poly);
        }
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