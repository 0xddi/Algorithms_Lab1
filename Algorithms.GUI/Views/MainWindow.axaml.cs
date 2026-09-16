using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Algorithms.Core;
using Algorithms.Core.MathFunctions;
using Algorithms.Core.PolynomialAlgorithms;
using Algorithms.Core.PowFunctionAlgorithms;
using ScottPlot.Avalonia;

namespace Algorithms.GUI.Views;

public partial class MainWindow : Window
{
    private Benchmarker _benchmarker;
    private readonly List<AvaPlot> _activePlots = new(); 
    
    public ObservableCollection<AlgorithmTaskItem> AlgorithmItems { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeTasksList();
    }

    private void InitializeTasksList()
    {
        void RegisterTaskInfo(string name, Func<double[], double> measurement)
        {
            var task = new BenchmarkTask(name, measurement);
            AlgorithmItems.Add(new AlgorithmTaskItem { Name = name, IsSelected = true, Task = task });
        }

        // 1. Сортировки
        RegisterTaskInfo("Bubble Sort", slice => new BubbleSortAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Quick Sort", slice => new QuickSortAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Tim Sort", slice => new TimSortAlgorithm(slice).RunBench(5));

        // 2. Математические функции
        RegisterTaskInfo("Constant Function", slice => new ConstantFunctionAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Sum Algorithm", slice => new SumAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Product Algorithm", slice => new ProductAlgorithm(slice).RunBench(5));

        // 3. Полиномы
        RegisterTaskInfo("Naive Polynomial", slice => new NaivePolynomialAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Horner Polynomial", slice => new HornerPolynomialAlgorithm(slice).RunBench(5));

        // 4. Степени
        const double baseX = 1.5;
        RegisterTaskInfo("Simple Pow (x^n)", slice => new SimplePowAlgorithm((x: baseX, n: slice.Length)).RunBench(5));
        RegisterTaskInfo("Recursive Pow", slice => new RecursivePowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5));
        RegisterTaskInfo("Fast Pow", slice => new FastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5));
        RegisterTaskInfo("Classic Fast Pow", slice => new ClassicFastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5));

        AlgorithmsList.ItemsSource = AlgorithmItems;
    }

    private double[] GenerateDataFromUI()
    {
        if (RangeDataRadio.IsChecked == true)
        {
            if (double.TryParse(StartValBox.Text, out double start) &&
                double.TryParse(EndValBox.Text, out double end) &&
                double.TryParse(StepValBox.Text, out double step) && step > 0)
            {
                int count = (int)Math.Max(0, (end - start) / step + 1);
                double[] arr = new double[count];
                for (int i = 0; i < count; i++)
                    arr[i] = start + i * step;
                return arr;
            }
        }
        else if (RandomDataRadio.IsChecked == true)
        {
            if (int.TryParse(CountValBox.Text, out int randomCount) && randomCount > 0)
            {
                double[] arr = new double[randomCount];
                for (int i = 0; i < randomCount; i++)
                    arr[i] = Random.Shared.NextDouble() * 100.0;
                return arr;
            }
        }
        
        return Array.Empty<double>(); 
    }

    private async void RunButton_Click(object? sender, RoutedEventArgs e)
    {
        var selectedTasks = AlgorithmItems.Where(item => item.IsSelected == true).Select(item => item.Task).ToList();

        if (selectedTasks.Count == 0)
        {
            StatusText.Text = "Выберите хотя бы один алгоритм!";
            return;
        }

        double[] inputData = GenerateDataFromUI();
        if (inputData.Length == 0)
        {
            StatusText.Text = "Ошибка ввода параметров данных! Проверьте параметры.";
            return;
        }

        RunButton.IsEnabled = false;
        ProgressIndicator.IsVisible = true;
        StatusText.Text = $"Выполняются замеры ({inputData.Length} элементов)...";
        PlotsPanel.Children.Clear();
        _activePlots.Clear();

        _benchmarker = new Benchmarker(inputData);
        
        foreach (var task in selectedTasks)
        {
            task.Results.Clear();
            _benchmarker.AddTask(task);
        }

        bool useCache = UseCacheCheckBox.IsChecked ?? true;

        await Task.Run(() => 
        {
            _benchmarker.RunFiltered(selectedTasks, useCache: useCache, benchCycles: 5);
        });

        RenderIndividualCharts(selectedTasks);

        RunButton.IsEnabled = true;
        ProgressIndicator.IsVisible = false;
        StatusText.Text = "Вычисления успешно завершены!";
    }

    private void RenderIndividualCharts(List<BenchmarkTask> tasks)
    {
        foreach (var task in tasks)
        {
            var border = new Border
            {
                Width = 460, Height = 320, Margin = new Avalonia.Thickness(8),
                Padding = new Avalonia.Thickness(8), 
                Background = SolidColorBrush.Parse("#252526"),
                BorderBrush = SolidColorBrush.Parse("#3E3E3E"), 
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6)
            };

            var plotControl = new AvaPlot { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            
            // Настройка тёмной темы для ScottPlot
            plotControl.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#252526");
            plotControl.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotControl.Plot.Axes.Color(ScottPlot.Color.FromHex("#DCDCDC"));

            double[] xs = task.Results.Select(r => (double)r.N).ToArray();
            double[] ys = task.Results.Select(r => r.TimeMs).ToArray();

            if (xs.Length > 0 && ys.Length > 0)
            {
                var scatter = plotControl.Plot.Add.ScatterLine(xs, ys);
                scatter.LineWidth = 2;
                scatter.Color = ScottPlot.Color.FromHex("#009688"); // Яркий бирюзовый цвет линии
                plotControl.Plot.Axes.AutoScale();
            }

            plotControl.Plot.Title(task.Name);
            plotControl.Plot.XLabel("Размер массива (N)");
            plotControl.Plot.YLabel("Время (мс)");
            plotControl.Refresh();

            border.Child = plotControl;
            PlotsPanel.Children.Add(border);
            _activePlots.Add(plotControl);
        }
    }

    private void SelectAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var item in AlgorithmItems) item.IsSelected = true;
    }

    private void DeselectAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var item in AlgorithmItems) item.IsSelected = false;
    }

    private void ZoomInX_Click(object? sender, RoutedEventArgs e) => ZoomPlots(1.2, 1.0);
    private void ZoomOutX_Click(object? sender, RoutedEventArgs e) => ZoomPlots(0.8, 1.0);
    private void ZoomInY_Click(object? sender, RoutedEventArgs e) => ZoomPlots(1.0, 1.2);
    private void ZoomOutY_Click(object? sender, RoutedEventArgs e) => ZoomPlots(1.0, 0.8);

    private void ZoomPlots(double fracX, double fracY)
    {
        foreach (var plot in _activePlots)
        {
            plot.Plot.Axes.Zoom(fracX, fracY);
            plot.Refresh();
        }
    }

    private void ResetZoom_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var plot in _activePlots)
        {
            plot.Plot.Axes.AutoScale();
            plot.Refresh();
        }
    }
    
    private void CompareHistoryButton_Click(object? sender, RoutedEventArgs e)
{
    PlotsPanel.Children.Clear();
    _activePlots.Clear();

    using var db = new Algorithms.Core.Database.AppDbContext();
    
    // Получаем уникальные алгоритмы, которые запускались ранее
    var historyAlgorithms = db.Results.Select(r => r.AlgorithmName).Distinct().ToList();

    foreach (var algoName in historyAlgorithms)
    {
        // Группируем по дате эксперимента
        var sessions = db.Results
            .Where(r => r.AlgorithmName == algoName)
            .OrderBy(r => r.N)
            .ToList()
            .GroupBy(r => r.ExperimentDate.ToString("g"))
            .ToList();

        if (!sessions.Any()) continue;

        var border = new Border { /* ... Те же настройки, что и в RenderIndividualCharts ... */ };
        var plotControl = new AvaPlot { HorizontalAlignment = HorizontalAlignment.Stretch };
        
        plotControl.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#252526");
        plotControl.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
        plotControl.Plot.Axes.Color(ScottPlot.Color.FromHex("#DCDCDC"));

        // Рисуем линию для каждой исторической сессии
        foreach (var session in sessions)
        {
            double[] xs = session.Select(r => (double)r.N).ToArray();
            double[] ys = session.Select(r => r.ElapsedTimeMs).ToArray();

            var scatter = plotControl.Plot.Add.ScatterLine(xs, ys);
            scatter.LineWidth = 2;
            scatter.Label = session.Key; // Помечаем легенду датой
        }

        plotControl.Plot.ShowLegend(); // Включаем легенду
        plotControl.Plot.Title($"{algoName} (Сравнение истории)");
        plotControl.Plot.XLabel("Размер массива (N)");
        plotControl.Plot.YLabel("Время (мс)");
        plotControl.Refresh();

        border.Child = plotControl;
        PlotsPanel.Children.Add(border);
        _activePlots.Add(plotControl);
    }
    
    StatusText.Text = "История загружена!";
}
}