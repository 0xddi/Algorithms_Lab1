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
using Algorithms.Core.MatrixAlgorithms;
using Algorithms.Core.PolynomialAlgorithms;
using Algorithms.Core.PowFunctionAlgorithms;
using Algorithms.GUI.Models;
using ScottPlot.Avalonia;

namespace Algorithms.GUI.Views;

public partial class MainWindow : Window
{
    private Benchmarker? _benchmarker;
    private readonly List<AvaPlot> _activePlots = new();
    private List<BenchmarkTask> _lastExecutedTasks = new();
    private List<MatrixBenchmarkResult> _lastMatrixResults = new();

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
        RegisterTaskInfo("Recursive Pow",
            slice => new RecursivePowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5));
        RegisterTaskInfo("Fast Pow", slice => new FastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5));
        RegisterTaskInfo("Classic Fast Pow",
            slice => new ClassicFastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5));

        AlgorithmsList.ItemsSource = AlgorithmItems;
    }

    /// <summary>
    /// Возвращает значение теоретической функции сложности f(n) для алгоритма
    /// </summary>
    private static double GetTheoreticalComplexity(string algoName, double n)
    {
        if (n <= 0) n = 1;
        return algoName switch
        {
            "Bubble Sort" or "Naive Polynomial" => n * n, // O(n^2)
            "Quick Sort" or "Tim Sort" => n * Math.Log2(Math.Max(n, 1.0001)), // O(n log n)
            "Sum Algorithm" or "Product Algorithm" or "Horner Polynomial"
                or "Simple Pow (x^n)" or "Recursive Pow" => n, // O(n)
            "Fast Pow" or "Classic Fast Pow" => Math.Log2(Math.Max(n, 1.0001)), // O(log n)
            "Constant Function" => 1.0, // O(1)
            _ => n
        };
    }

    /// <summary>
    /// Расчет аппроксимирующей функции T_approx(n) = C * f(n) по МНК и среднеквадратичной ошибки (MSE)
    /// </summary>
    private static (double C, double Mse, double[] YApprox) FitApproximation(double[] xs, double[] ys, string algoName)
    {
        int k = xs.Length;
        if (k == 0) return (0, 0, Array.Empty<double>());

        double sumNumerator = 0;
        double sumDenominator = 0;
        double[] fn = new double[k];

        for (int i = 0; i < k; i++)
        {
            fn[i] = GetTheoreticalComplexity(algoName, xs[i]);
            sumNumerator += ys[i] * fn[i];
            sumDenominator += fn[i] * fn[i];
        }

        double C = sumDenominator > 1e-12 ? sumNumerator / sumDenominator : 0;
        double[] yApprox = new double[k];
        double sumSquaredError = 0;

        for (int i = 0; i < k; i++)
        {
            yApprox[i] = C * fn[i];
            double err = ys[i] - yApprox[i];
            sumSquaredError += err * err;
        }

        double mse = sumSquaredError / k;
        return (C, mse, yApprox);
    }

    /// <summary>
    /// Теоретическое число операций для наивного умножения A(n×m) на B(m×n): n*m*n = n²·m
    /// </summary>
    private static double GetMatrixTheoreticalComplexity(int n, int m) => (double)n * n * m;

    private static (double C, double Mse) FitMatrixApproximation(IReadOnlyList<MatrixBenchmarkResult> results)
    {
        int k = results.Count;
        if (k == 0) return (0, 0);

        double sumNumerator = 0, sumDenominator = 0;
        var fn = new double[k];

        for (int i = 0; i < k; i++)
        {
            fn[i] = GetMatrixTheoreticalComplexity(results[i].N, results[i].M);
            sumNumerator += results[i].TimeMs * fn[i];
            sumDenominator += fn[i] * fn[i];
        }

        double C = sumDenominator > 1e-12 ? sumNumerator / sumDenominator : 0;

        double sumSquaredError = 0;
        for (int i = 0; i < k; i++)
        {
            double approx = C * fn[i];
            double err = results[i].TimeMs - approx;
            sumSquaredError += err * err;
        }

        return (C, sumSquaredError / k);
    }
    
    private void RenderMatrixPanel(List<MatrixBenchmarkResult> results, string label = "Умножение матриц (n×m)")
    {
        if (results.Count == 0) return;

        var border = new Border
        {
            Width = 620, Height = 480, Margin = new Avalonia.Thickness(8),
            Padding = new Avalonia.Thickness(4),
            Background = SolidColorBrush.Parse("#252526"),
            BorderBrush = SolidColorBrush.Parse("#3E3E3E"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6)
        };

        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = SolidColorBrush.Parse("#DCDCDC"),
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Margin = new Avalonia.Thickness(4, 2, 0, 0)
        });

        var control = new MatrixSurfaceControl { Height = 440 };
        stack.Children.Add(control);

        border.Child = stack;
        PlotsPanel.Children.Add(border);
        control.SetResults(results);
    }
    
    private void RenderMatrixHeatmap(List<MatrixBenchmarkResult> results, string titleSuffix = "")
    {
        if (results.Count == 0) return;

        var nValues = results.Select(r => r.N).Distinct().OrderBy(x => x).ToArray();
        var mValues = results.Select(r => r.M).Distinct().OrderBy(x => x).ToArray();

        // data[row, col]: строки — значения m, столбцы — значения n
        var data = new double[mValues.Length, nValues.Length];
        foreach (var r in results)
        {
            int row = Array.IndexOf(mValues, r.M);
            int col = Array.IndexOf(nValues, r.N);
            data[row, col] = r.TimeMs;
        }

        var border = new Border
        {
            Width = 460, Height = 320, Margin = new Avalonia.Thickness(8),
            Padding = new Avalonia.Thickness(8),
            Background = SolidColorBrush.Parse("#252526"),
            BorderBrush = SolidColorBrush.Parse("#3E3E3E"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6)
        };

        var plotControl = new AvaPlot
            { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        plotControl.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#252526");
        plotControl.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
        plotControl.Plot.Axes.Color(ScottPlot.Color.FromHex("#DCDCDC"));

        var heatmap = plotControl.Plot.Add.Heatmap(data);
        heatmap.Colormap = new ScottPlot.Colormaps.Viridis();
        heatmap.Extent = new ScottPlot.CoordinateRect(nValues.Min(), nValues.Max(), mValues.Min(), mValues.Max());
        plotControl.Plot.Add.ColorBar(heatmap);

        string title = "Умножение матриц (n×m)" + titleSuffix;
        if (ShowApproxCheckBox.IsChecked ?? true)
        {
            var (_, mse) = FitMatrixApproximation(results);
            title += $" (MSE: {mse:E2})";
        }

        plotControl.Plot.Title(title);
        plotControl.Plot.XLabel("n");
        plotControl.Plot.YLabel("m");
        plotControl.Plot.Axes.AutoScale();
        plotControl.Refresh();

        border.Child = plotControl;
        PlotsPanel.Children.Add(border);
        _activePlots.Add(plotControl);
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

        bool runMatrix = RunMatrixCheckBox.IsChecked ?? true;

        if (selectedTasks.Count == 0 && !runMatrix)
        {
            StatusText.Text = "Выберите хотя бы один алгоритм!";
            return;
        }

        double[] inputData = GenerateDataFromUI();
        if (selectedTasks.Count > 0 && inputData.Length == 0)
        {
            StatusText.Text = "Ошибка ввода параметров данных! Проверьте параметры.";
            return;
        }

        RunButton.IsEnabled = false;
        ProgressIndicator.IsVisible = true;
        StatusText.Text = "Выполняются замеры...";
        PlotsPanel.Children.Clear();
        _activePlots.Clear();

        _benchmarker = new Benchmarker(inputData);

        foreach (var task in selectedTasks)
        {
            task.Results.Clear();
            _benchmarker.AddTask(task);
        }

        bool useCache = UseCacheCheckBox.IsChecked ?? true;

        int matrixNMax = int.TryParse(MatrixNMaxBox.Text, out var nm) && nm > 0 ? nm : 200;
        int matrixMMax = int.TryParse(MatrixMMaxBox.Text, out var mm) && mm > 0 ? mm : 200;
        int matrixStep = int.TryParse(MatrixStepBox.Text, out var ms) && ms > 0 ? ms : 10;

        List<MatrixBenchmarkResult>? matrixResults = null;

        await Task.Run(() =>
        {
            if (selectedTasks.Count > 0)
            {
                _benchmarker.RunFiltered(selectedTasks, useCache: useCache, benchCycles: 5);
            }

            if (runMatrix)
            {
                var matrixBench = new MatrixBenchmarker("Matrix Multiplication (naive)", (n, m) =>
                {
                    var a = MatrixUtils.GenerateRandomMatrix(n, m);
                    var b = MatrixUtils.GenerateRandomMatrix(m, n);
                    return new MatrixMultiplicationAlgorithm((a, b)).RunBench(5);
                });

                var nValues = Enumerable.Range(1, matrixNMax / matrixStep).Select(i => i * matrixStep);
                var mValues = Enumerable.Range(1, matrixMMax / matrixStep).Select(i => i * matrixStep);

                matrixBench.Run(nValues, mValues, useCache: useCache);
                matrixResults = matrixBench.Results;
            }
        });

        _lastExecutedTasks = selectedTasks;
        _lastMatrixResults = matrixResults ?? new List<MatrixBenchmarkResult>();
        CurrentLoadedSessions.Clear();

        RenderIndividualCharts(selectedTasks);
        if (_lastMatrixResults.Any())
        {
            RenderMatrixPanel(_lastMatrixResults);
        }

        _historyWindow?.LoadHistoryFromDb();

        RunButton.IsEnabled = true;
        ProgressIndicator.IsVisible = false;
        StatusText.Text = "Вычисления успешно завершены!";
    }

    private void RenderIndividualCharts(List<BenchmarkTask> tasks)
    {
        PlotsPanel.Children.Clear();
        _activePlots.Clear();

        bool showApprox = ShowApproxCheckBox.IsChecked ?? true;

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

            var plotControl = new AvaPlot
                { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

            plotControl.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#252526");
            plotControl.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotControl.Plot.Axes.Color(ScottPlot.Color.FromHex("#DCDCDC"));

            double[] xs = task.Results.Select(r => (double)r.N).ToArray();
            double[] ys = task.Results.Select(r => r.TimeMs).ToArray();

            if (xs.Length > 0 && ys.Length > 0)
            {
                var empiricalScatter = plotControl.Plot.Add.Scatter(xs, ys);
                empiricalScatter.LineWidth = 2;
                empiricalScatter.Color = ScottPlot.Color.FromHex("#009688");
                empiricalScatter.LegendText = "Эксперимент";

                if (showApprox)
                {
                    var (c, mse, yApprox) = FitApproximation(xs, ys, task.Name);
                    var approxScatter = plotControl.Plot.Add.Scatter(xs, yApprox);
                    approxScatter.LineWidth = 2;
                    approxScatter.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                    approxScatter.Color = ScottPlot.Color.FromHex("#FF9800"); // Выделяющийся оранжевый цвет
                    approxScatter.MarkerSize = 0; // Линия без маркеров
                    approxScatter.LegendText = $"Теория (MSE: {mse:E2})";

                    plotControl.Plot.ShowLegend();
                    plotControl.Plot.Legend.Alignment = ScottPlot.Alignment.UpperLeft;
                }

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

    /// <summary>
    ///При переключении галочки перерисовываем актуальные графики
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ShowApprox_Click(object? sender, RoutedEventArgs e)
    {
        if (CurrentLoadedSessions.Any())
        {
            RenderComparisonCharts(CurrentLoadedSessions);
        }
        else if (_lastExecutedTasks.Any() || _lastMatrixResults.Any())
        {
            RenderIndividualCharts(_lastExecutedTasks);
            if (_lastMatrixResults.Any()) RenderMatrixPanel(_lastMatrixResults);
        }
    }
    
    private void SelectAll_Click(object? sender, RoutedEventArgs e) =>
        AlgorithmItems.ToList().ForEach(i => i.IsSelected = true);

    private void DeselectAll_Click(object? sender, RoutedEventArgs e) =>
        AlgorithmItems.ToList().ForEach(i => i.IsSelected = false);

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

    bool showApprox = ShowApproxCheckBox.IsChecked ?? true;
    var allResults = sessionsToCompare.SelectMany(s => (IEnumerable<ExperimentResult>)s.Results).ToList();
    var uniqueAlgorithms = allResults.Select(r => r.AlgorithmName).Distinct().ToList();

    var colors = new[] { "#009688", "#E91E63", "#FFC107", "#2196F3", "#9C27B0", "#4CAF50", "#FF5722" };

    foreach (var algoName in uniqueAlgorithms)
    {
        var algoAllResults = allResults.Where(r => r.AlgorithmName == algoName).ToList();
        bool isMatrixAlgo = algoAllResults.Any(r => r.M.HasValue);

        if (isMatrixAlgo)
        {
            foreach (var session in sessionsToCompare)
            {
                var sessionMatrixResults = session.Results
                    .Where(r => r.AlgorithmName == algoName && r.M.HasValue)
                    .Select(r => new MatrixBenchmarkResult(r.N, r.M!.Value, r.ElapsedTimeMs))
                    .ToList();

                if (sessionMatrixResults.Any())
                {
                    RenderMatrixPanel(sessionMatrixResults, $"Умножение матриц — {session.Date:g}");
                }
            }

            continue; // <-- теперь continue закрывает именно этот if, ничего больше не "прячет"
        }

        var border = new Border
        {
            Width = 460, Height = 320, Margin = new Avalonia.Thickness(8),
            Padding = new Avalonia.Thickness(8),
            Background = SolidColorBrush.Parse("#252526"),
            BorderBrush = SolidColorBrush.Parse("#3E3E3E"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6)
        };

        var plotControl = new AvaPlot
        {
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
        };
        plotControl.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#252526");
        plotControl.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
        plotControl.Plot.Axes.Color(ScottPlot.Color.FromHex("#DCDCDC"));

        var sessionsWithAlgo = sessionsToCompare.Where(s => s.Results.Any(r => r.AlgorithmName == algoName)).ToList();
        bool isShared = sessionsWithAlgo.Count > 1;

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
                scatter.Color = ScottPlot.Color.FromHex(colors[i % colors.Length]);
                scatter.LegendText = $"№ {i + 1}";
            }
            else
            {
                scatter.Color = ScottPlot.Color.FromHex("#009688");
            }

            if (showApprox && xs.Length > 0)
            {
                var (c, mse, yApprox) = FitApproximation(xs, ys, algoName);
                var approxScatter = plotControl.Plot.Add.Scatter(xs, yApprox);
                approxScatter.LineWidth = 1.5f;
                approxScatter.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                approxScatter.Color = ScottPlot.Color.FromHex("#FF9800");
                approxScatter.MarkerSize = 0;
                approxScatter.LegendText = isShared ? $"Теория №{i + 1} (MSE: {mse:E1})" : $"Теория (MSE: {mse:E1})";
            }
        }

        plotControl.Plot.ShowLegend();
        plotControl.Plot.Legend.Alignment = ScottPlot.Alignment.LowerRight;

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