using System.Linq;
using Avalonia.Controls;
using Algorithms.Core;

namespace Algorithms.GUI.Views;

public partial class ChartWindow : Window
{
    private readonly BenchmarkTask? _task;

    // Пустой конструктор нужен дизайнеру XAML
    public ChartWindow()
    {
        InitializeComponent();
    }

    public ChartWindow(BenchmarkTask task) : this()
    {
        _task = task;
        Title = $"График: {task.Name}";
        RenderChart();
    }

    private void RenderChart()
    {
        if (_task is null || _task.Results.Count == 0)
            return;

        double[] xs = _task.Results.Select(r => (double)r.N).ToArray();
        double[] ys = _task.Results.Select(r => r.TimeMs).ToArray();

        var scatter = AvaPlot1.Plot.Add.ScatterLine(xs, ys);
        scatter.LegendText = _task.Name;
        scatter.LineWidth = 2;

        AvaPlot1.Plot.Title($"Асимптотическая сложность: {_task.Name}");
        AvaPlot1.Plot.XLabel("Размер массива (N)");
        AvaPlot1.Plot.YLabel("Время выполнения (мс)");
        AvaPlot1.Plot.ShowLegend();
        AvaPlot1.Plot.Axes.AutoScale();
        AvaPlot1.Refresh();
    }
}