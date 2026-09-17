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
using Algorithms.Core.Database;
using Algorithms.Core.MathFunctions;
using Algorithms.Core.PolynomialAlgorithms;
using Algorithms.Core.PowFunctionAlgorithms;
using Algorithms.GUI.Models;
using ScottPlot.Avalonia;

namespace Algorithms.GUI.Views;

public partial class MainWindow : Window
{
    private Benchmarker? _benchmarker;
    private readonly List<AvaPlot> _activePlots = new(); 
    
    // Добавлена ссылка на открытое окно истории для его обновления
    private HistoryWindow? _historyWindow;
    
    public List<HistorySession> CurrentLoadedSessions { get; set; } = new();
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

        RegisterTaskInfo("Bubble Sort", slice => new BubbleSortAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Quick Sort", slice => new QuickSortAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Tim Sort", slice => new TimSortAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Constant Function", slice => new ConstantFunctionAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Sum Algorithm", slice => new SumAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Product Algorithm", slice => new ProductAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Naive Polynomial", slice => new NaivePolynomialAlgorithm(slice).RunBench(5));
        RegisterTaskInfo("Horner Polynomial", slice => new HornerPolynomialAlgorithm(slice).RunBench(5));

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
        
        // Автоматически обновляем окно истории (если оно открыто)
        _historyWindow?.LoadHistoryFromDb();

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
            
            plotControl.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#252526");
            plotControl.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotControl.Plot.Axes.Color(ScottPlot.Color.FromHex("#DCDCDC"));

            double[] xs = task.Results.Select(r => (double)r.N).ToArray();
            double[] ys = task.Results.Select(r => r.TimeMs).ToArray();

            if (xs.Length > 0 && ys.Length > 0)
            {
                var scatter = plotControl.Plot.Add.Scatter(xs, ys);
                scatter.LineWidth = 2;
                scatter.Color = ScottPlot.Color.FromHex("#009688"); // Отрисовка как обычно
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

    private void SelectAll_Click(object? sender, RoutedEventArgs e) => AlgorithmItems.ToList().ForEach(i => i.IsSelected = true);
    private void DeselectAll_Click(object? sender, RoutedEventArgs e) => AlgorithmItems.ToList().ForEach(i => i.IsSelected = false);
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
        // Проверяем, открыто ли уже окно. Если нет — создаем, если да — выводим на передний план.
        if (_historyWindow == null)
        {
            _historyWindow = new HistoryWindow(this);
            _historyWindow.Closed += (s, args) => _historyWindow = null;
            _historyWindow.Show();
        }
        else
        {
            _historyWindow.Activate();
        }
    }
    
    public void RenderComparisonCharts(List<HistorySession> sessionsToCompare)
    {
        PlotsPanel.Children.Clear();
        _activePlots.Clear();

        if (sessionsToCompare == null || !sessionsToCompare.Any()) return;

        var allResults = sessionsToCompare.SelectMany(s => (IEnumerable<ExperimentResult>)s.Results).ToList();
        var uniqueAlgorithms = allResults.Select(r => r.AlgorithmName).Distinct().ToList();

        var colors = new[] { "#009688", "#E91E63", "#FFC107", "#2196F3", "#9C27B0", "#4CAF50", "#FF5722" };

        foreach (var algoName in uniqueAlgorithms)
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
            plotControl.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#252526");
            plotControl.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotControl.Plot.Axes.Color(ScottPlot.Color.FromHex("#DCDCDC"));

            // Проверяем, совпадает ли алгоритм в сравниваемых данных (встречается ли он > 1 раза)
            var sessionsWithAlgo = sessionsToCompare.Where(s => s.Results.Any(r => r.AlgorithmName == algoName)).ToList();
            bool isShared = sessionsWithAlgo.Count > 1;

            // Индексация привязана строго к сессии для сохранения целостности номеров (№1, №2, и т.д.)
            for (int i = 0; i < sessionsToCompare.Count; i++)
            {
                var session = sessionsToCompare[i];
                var sessionAlgoResults = session.Results
                    .Where(r => r.AlgorithmName == algoName)
                    .OrderBy(r => r.N)
                    .ToList();

                if (!sessionAlgoResults.Any()) continue;

                double[] xs = sessionAlgoResults.Select(r => (double)r.N).ToArray();
                double[] ys = sessionAlgoResults.Select(r => r.ElapsedTimeMs).ToArray();

                var scatter = plotControl.Plot.Add.Scatter(xs, ys);
                scatter.LineWidth = 2;

                if (isShared)
                {
                    // Пункт 1: совпадающие алгоритмы
                    scatter.Color = ScottPlot.Color.FromHex(colors[i % colors.Length]);
                    scatter.LegendText = $"№ {i + 1}"; // Подписываем в соответствии со своими номерами
                }
                else
                {
                    // Пункт 2: не совпадающие просто рисуются как обычно (стандартный цвет, без подписи)
                    scatter.Color = ScottPlot.Color.FromHex("#009688"); 
                }
            }

            if (isShared)
            {
                plotControl.Plot.ShowLegend();
                plotControl.Plot.Legend.Alignment = ScottPlot.Alignment.LowerRight; // Размещаем подписи в правом нижнем углу
            }
            
            plotControl.Plot.Title(algoName, size: null);
            plotControl.Plot.XLabel("Размер массива (N)");
            plotControl.Plot.YLabel("Время (мс)");
            
            plotControl.Plot.Axes.AutoScale();
            plotControl.Refresh();

            border.Child = plotControl;
            PlotsPanel.Children.Add(border);
            _activePlots.Add(plotControl);
        }
        
        StatusText.Text = $"Отображено данных на графиках: {sessionsToCompare.Count} сессий";
    }
}