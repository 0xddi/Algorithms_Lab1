using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.Core.MatrixAlgorithms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

namespace Algorithms.GUI.Views;

public partial class MatrixSurfaceWindow : Window
{
    private int[] _nValues = Array.Empty<int>();
    private int[] _mValues = Array.Empty<int>();
    private double[,] _grid = new double[0, 0];
    private double _maxTime;

    private double _zoom = 1.0;
    private double _panOffsetX;
    private double _panOffsetY;
    private bool _isPanning;
    private Point _lastPointerPosition;

    public MatrixSurfaceWindow() => InitializeComponent();

    public MatrixSurfaceWindow(List<MatrixBenchmarkResult> results) : this()
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
        LegendMaxText.Text = $"{_maxTime:F2}";
        LegendMinText.Text = "0";

        Opened += (_, _) => DrawSurface(RotationSlider.Value);
    }

    private void RotationSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        => DrawSurface(e.NewValue);

    private void DrawCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        double zoomFactor = e.Delta.Y > 0 ? 1.1 : 0.9;
        _zoom = Math.Clamp(_zoom * zoomFactor, 0.2, 5.0);
        DrawSurface(RotationSlider.Value);
    }

    private void DrawCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isPanning = true;
        _lastPointerPosition = e.GetPosition(DrawCanvas);
    }

    private void DrawCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning) return;
        var pos = e.GetPosition(DrawCanvas);
        _panOffsetX += pos.X - _lastPointerPosition.X;
        _panOffsetY += pos.Y - _lastPointerPosition.Y;
        _lastPointerPosition = pos;
        DrawSurface(RotationSlider.Value);
    }

    private void DrawCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => _isPanning = false;

    private void DrawSurface(double angleDegrees)
    {
        DrawCanvas.Children.Clear();

        int rows = _nValues.Length;
        int cols = _mValues.Length;
        if (rows < 2 || cols < 2) return;

        double angle = angleDegrees * Math.PI / 180.0;
        double cosA = Math.Cos(angle);
        double sinA = Math.Sin(angle);

        double canvasW = DrawCanvas.Bounds.Width > 0 ? DrawCanvas.Bounds.Width : 860;
        double canvasH = DrawCanvas.Bounds.Height > 0 ? DrawCanvas.Bounds.Height : 640;

        double scale = 6.0 * _zoom;
        double heightScale = (_maxTime > 0 ? 150.0 / _maxTime : 1.0) * _zoom;
        double offsetX = canvasW / 2 + _panOffsetX;
        double offsetY = canvasH * 0.75 + _panOffsetY;

        (double sx, double sy) Project(double x, double z, double y)
        {
            double rx = x * cosA - z * sinA;
            double rz = x * sinA + z * cosA;

            double screenX = (rx - rz) * scale * Math.Cos(Math.PI / 6) + offsetX;
            double screenY = (rx + rz) * scale * Math.Sin(Math.PI / 6) - y * heightScale + offsetY;
            return (screenX, screenY);
        }

        // --- Опорная плоскость (пол) на высоте 0, чтобы легче считывалась перспектива ---
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

        // --- Подписи осей: минимум и максимум по n и по m ---
        AddAxisLabel(Project(0, 0, 0), $"n={_nValues[0]}, m={_mValues[0]}");
        AddAxisLabel(Project(rows - 1, 0, 0), $"n={_nValues[^1]}");
        AddAxisLabel(Project(0, cols - 1, 0), $"m={_mValues[^1]}");

        // --- Сама поверхность ---
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
            FontSize = 11
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