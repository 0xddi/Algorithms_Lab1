using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
using Algorithms.GUI.Themes;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using ScottPlot.Avalonia;

namespace Algorithms.GUI.Views;

public partial class MainWindow : Window
{
    private Benchmarker? _benchmarker;
    private readonly List<AvaPlot> _activePlots = new();
    private List<BenchmarkTask> _lastExecutedTasks = new();
    private List<MatrixBenchmarkResult> _lastMatrixResults = new();

    private HistoryWindow? _historyWindow;

    private CancellationTokenSource? _cancellationTokenSource;
    private DateTime _benchmarkStartTime;

    public List<HistorySession> CurrentLoadedSessions { get; set; } = new();
    public ObservableCollection<AlgorithmTaskItem> AlgorithmItems { get; } = new();
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeTasksList();

        SyncThemeToggle();
        if (Avalonia.Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += OnThemeChanged;
            Closed += (_, _) => app.ActualThemeVariantChanged -= OnThemeChanged;
        }
    }

    // ===== Тема оформления =====

    private bool _syncingThemeToggle;

    private void SyncThemeToggle()
    {
        _syncingThemeToggle = true;
        ThemeToggle.IsChecked = ThemeColors.IsLight;
        _syncingThemeToggle = false;
    }

    private void ThemeToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_syncingThemeToggle || Avalonia.Application.Current is not { } app) return;

        var variant = ThemeToggle.IsChecked == true ? ThemeVariant.Light : ThemeVariant.Dark;
        app.RequestedThemeVariant = variant;
        ThemeSettings.Save(variant);
    }

    /// <summary>
    /// Fires for both the toggle and a system theme change (while following the system).
    /// XAML updates itself via DynamicResource; charts are built in code, so they are re-rendered.
    /// </summary>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        SyncThemeToggle();
        RerenderCharts();
    }

    private static void ApplyPlotTheme(AvaPlot plotControl)
    {
        var plot = plotControl.Plot;
        plot.FigureBackground.Color = ThemeColors.Plot(ThemeColors.PanelBackground);
        plot.DataBackground.Color = ThemeColors.Plot(ThemeColors.PlotDataBackground);
        plot.Axes.Color(ThemeColors.Plot(ThemeColors.Axes));

        // Data first: thin, low-contrast grid and no top/right frame lines competing with the series
        plot.Grid.MajorLineColor = ThemeColors.Plot(ThemeColors.GridLine);
        plot.Grid.MajorLineWidth = 1;
        plot.Axes.Top.FrameLineStyle.Width = 0;
        plot.Axes.Right.FrameLineStyle.Width = 0;

        // Text hierarchy: the title in the main text color, axis labels a step quieter
        plot.Axes.Title.Label.ForeColor = ThemeColors.Plot(ThemeColors.Text);
        plot.Axes.Title.Label.FontSize = 15;
        plot.Axes.Title.Label.Bold = true;
        plot.Axes.Bottom.Label.ForeColor = ThemeColors.Plot(ThemeColors.TextSecondary);
        plot.Axes.Left.Label.ForeColor = ThemeColors.Plot(ThemeColors.TextSecondary);
        plot.Axes.Bottom.Label.FontSize = 13;
        plot.Axes.Left.Label.FontSize = 13;
        plot.Axes.Bottom.Label.Bold = false;
        plot.Axes.Left.Label.Bold = false;

        // A little air around the data so the first and last points are not glued to the axes
        plot.Axes.Margins(0.03, 0.08);
    }

    // Marker shapes for series on the combined chart: a second cue besides color
    private static readonly ScottPlot.MarkerShape[] SeriesMarkers =
    {
        ScottPlot.MarkerShape.FilledCircle, ScottPlot.MarkerShape.FilledSquare, ScottPlot.MarkerShape.FilledTriangleUp,
        ScottPlot.MarkerShape.FilledDiamond, ScottPlot.MarkerShape.FilledTriangleDown, ScottPlot.MarkerShape.OpenCircle,
        ScottPlot.MarkerShape.OpenSquare, ScottPlot.MarkerShape.OpenDiamond
    };

    /// <summary>
    /// Adds a chart card to its panel and plays the entrance animation (skipped if the OS disables animations).
    /// </summary>
    private void AddCard(Panel host, Control card)
    {
        host.Children.Add(card);
        Motion.FadeIn(card, MatrixPanelHost.Children.Count + PlotsPanel.Children.Count - 1);
        UpdateEmptyState();
    }

    private void UpdateEmptyState() =>
        EmptyStatePanel.IsVisible = MatrixPanelHost.Children.Count == 0 && PlotsPanel.Children.Count == 0;

    private void InitializeTasksList()
    {
        void RegisterTaskInfo(string name, Func<double[], double> measurement, bool isStep = false)
        {
            var task = new BenchmarkTask(name, measurement) { IsStepMeasurement = isStep };
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
        RegisterTaskInfo("Selection Sort", slice => new SelectionSortAlgorithm(slice).RunBench(5)); // Добавлено

        const double baseX = 1.5;
        RegisterTaskInfo("Simple Pow (x^n)", slice => {
            var algo = new SimplePowAlgorithm((x: baseX, n: slice.Length));
            algo.Execute(); // Запустите алгоритм 1 раз (замените на ваш метод, если он называется иначе)
            return algo.Steps;
        }, isStep: true);

        RegisterTaskInfo("Recursive Pow", slice => {
            var algo = new RecursivePowerAlgorithm((x: baseX, n: slice.Length));
            algo.Execute();
            return algo.Steps;
        }, isStep: true);

        RegisterTaskInfo("Fast Pow", slice => {
            var algo = new FastPowerAlgorithm((x: baseX, n: slice.Length));
            algo.Execute();
            return algo.Steps;
        }, isStep: true);

        RegisterTaskInfo("Classic Fast Pow", slice => {
            var algo = new ClassicFastPowerAlgorithm((x: baseX, n: slice.Length));
            algo.Execute();
            return algo.Steps;
        }, isStep: true);
        
        
        
        RegisterTaskInfo("Aho-Corasick", slice => new AhoCorasickAlgorithm(slice).RunBench(5)); // Добавлено
        RegisterTaskInfo("Heap Sort", slice => new HeapSortAlgorithm(slice).RunBench(5));

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
            "Bubble Sort" or "Naive Polynomial" or "Selection Sort" => n * n, // O(n²)
            "Quick Sort" or "Tim Sort" or "Heap Sort" => n * Math.Log2(Math.Max(n, 1.0001)), // O(n log n)
            "Sum Algorithm" or "Product Algorithm" or "Horner Polynomial"
                or "Simple Pow (x^n)" or "Aho-Corasick" => n, // O(n)
            "Recursive Pow" or "Fast Pow" or "Classic Fast Pow"
                => Math.Log2(Math.Max(n, 1.0001)), // O(log n)
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

        bool showApprox = ShowApproxCheckBox.IsChecked ?? true;

        var border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 550,
            Margin = new Avalonia.Thickness(0),
            Padding = new Avalonia.Thickness(8),
            Background = ThemeColors.Brush(ThemeColors.PanelBackground),
            BorderBrush = ThemeColors.Brush(ThemeColors.Border),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(12)
        };

        var control = new MatrixSurfaceControl
        {
            Height = 480,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(BuildMatrixHeader(label, control));
        stack.Children.Add(control);

        border.Child = stack;
        AddCard(MatrixPanelHost, border);
        // Если галочка установлена — генерируем теоретическую поверхность
        if (showApprox)
        {
            var (c, mse) = FitMatrixApproximation(results);
            var approxResults = results
                .Select(r => new MatrixBenchmarkResult(r.N, r.M, c * GetMatrixTheoreticalComplexity(r.N, r.M)))
                .ToList();

            var seriesList = new List<MatrixSeries>
            {
                new MatrixSeries
                {
                    Name = "Эксперимент",
                    Results = results,
                    Color = Color.Parse(ThemeColors.Accent)
                },
                new MatrixSeries
                {
                    Name = $"Теория (MSE: {mse:E2})",
                    Results = approxResults,
                    Color = Color.Parse("#40" + ThemeColors.Theory[1..]) // Первые символы "40" задают высокую прозрачность
                }
            };

            control.SetMultipleResults(seriesList);
        }
        else
        {
            control.SetResults(results);
        }
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
            HorizontalAlignment = HorizontalAlignment.Stretch, Height = 360, Margin = new Avalonia.Thickness(0),
            Padding = new Avalonia.Thickness(8),
            Background = ThemeColors.Brush(ThemeColors.PanelBackground),
            BorderBrush = ThemeColors.Brush(ThemeColors.Border),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(12)
        };

        var plotControl = new AvaPlot
            { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        ApplyPlotTheme(plotControl);

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

        border.Child = WrapWithZoomToolbar(plotControl, plotControl);
        AddCard(PlotsPanel, border);
        _activePlots.Add(plotControl);
    }

    private double[] GenerateDataFromUI()
{
    if (RangeDataRadio.IsChecked == true)
    {
        if (double.TryParse(StartValBox.Text, out double start) &&
            double.TryParse(EndValBox.Text, out double end) &&
            double.TryParse(StepValBox.Text, out double step) &&
            step > 0)
        {
            const double tolerance = 1e-9;
            var values = new List<double>();

            double current = start;

            if (end >= start)
            {
                while (current <= end + tolerance)
                {
                    values.Add(current);

                    double next = current + step;

                    if (next > end + tolerance)
                    {
                        if (Math.Abs(values[^1] - end) > tolerance)
                        {
                            values.Add(end);
                        }

                        break;
                    }

                    current = next;
                }
            }
            else
            {
                while (current >= end - tolerance)
                {
                    values.Add(current);

                    double next = current - step;

                    if (next < end - tolerance)
                    {
                        if (Math.Abs(values[^1] - end) > tolerance)
                        {
                            values.Add(end);
                        }

                        break;
                    }

                    current = next;
                }
            }

            return values.ToArray();
        }
    }
    else if (RandomDataRadio.IsChecked == true)
    {
        if (int.TryParse(CountValBox.Text, out int randomCount) &&
            randomCount > 0)
        {
            double[] arr = new double[randomCount];

            for (int i = 0; i < randomCount; i++)
            {
                arr[i] = Random.Shared.NextDouble() * 100.0;
            }

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

        // Подготовка UI к запуску
        RunButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        ProgressPanel.IsVisible = true;
        ProgressIndicator.Value = 0;
        ProgressPercentText.Text = "0%";
        EtaText.Text = "Осталось: вычисление...";
        StatusText.Text = "Выполняются замеры...";
        PlotsPanel.Children.Clear();
        MatrixPanelHost.Children.Clear();
        UpdateEmptyState();
        _activePlots.Clear();

        _benchmarker = new Benchmarker(inputData);
        foreach (var task in selectedTasks)
        {
            task.Results.Clear();
            _benchmarker.AddTask(task);
        }

        bool useCache = UseCacheCheckBox.IsChecked ?? true;

        // Расчет параметров матриц для определения общего количества шагов
        int matrixNMax = int.TryParse(MatrixNMaxBox.Text, out var nm) && nm > 0 ? nm : 200;
        int matrixMMax = int.TryParse(MatrixMMaxBox.Text, out var mm) && mm > 0 ? mm : 200;
        int matrixStep = int.TryParse(MatrixStepBox.Text, out var ms) && ms > 0 ? ms : 10;

        var nValues = Enumerable.Range(1, matrixNMax / matrixStep).Select(i => i * matrixStep).ToList();
        var mValues = Enumerable.Range(1, matrixMMax / matrixStep).Select(i => i * matrixStep).ToList();

        // Подсчитываем 100% шагов для ETA
        int totalSteps = selectedTasks.Count * inputData.Length;
        if (runMatrix) totalSteps += nValues.Count * mValues.Count;

        var progressState = new BenchmarkProgressState { TotalSteps = totalSteps, CurrentStep = 0 };
        _benchmarkStartTime = DateTime.Now;

        // Обработчик изменения прогресса из фонового потока
        var progressReporter = new Progress<BenchmarkProgressState>(state =>
        {
            double percent = state.TotalSteps > 0 ? ((double)state.CurrentStep / state.TotalSteps) * 100.0 : 0;
            ProgressIndicator.Value = percent;
            ProgressPercentText.Text = $"{percent:F1}%";

            var elapsed = DateTime.Now - _benchmarkStartTime;
            if (percent > 0)
            {
                // Линейная аппроксимация оставшегося времени
                var totalEstimated = TimeSpan.FromTicks((long)(elapsed.Ticks / (percent / 100.0)));
                var remaining = totalEstimated - elapsed;
                EtaText.Text = $"Осталось: ~{remaining:hh\\:mm\\:ss}";
            }

            StatusText.Text = $"Текущая задача: {state.CurrentTaskName} ({state.CurrentStep}/{state.TotalSteps})";
        });

        // Создаем токен отмены
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        List<MatrixBenchmarkResult>? matrixResults = null;

        try
        {
            await Task.Run(() =>
            {
                if (selectedTasks.Count > 0)
                {
                    _benchmarker.RunFiltered(selectedTasks, useCache: useCache, benchCycles: 5,
                        token: token, progress: progressReporter, progressState: progressState);
                }

                if (runMatrix)
                {
                    var matrixBench = new MatrixBenchmarker("Matrix Multiplication (naive)", (n, m) =>
                    {
                        var a = MatrixUtils.GenerateRandomMatrix(n, m);
                        var b = MatrixUtils.GenerateRandomMatrix(m, n);
                        return new MatrixMultiplicationAlgorithm((a, b)).RunBench(1);
                    });

                    try
                    {
                        matrixBench.Run(nValues, mValues, useCache: useCache,
                            token: token, progress: progressReporter, progressState: progressState);
                    }
                    finally
                    {
                        // Забираем частично вычисленные результаты, даже если сработало прерывание (throw OperationCanceledException)
                        matrixResults = matrixBench.Results;
                    }
                }
            }, token);

            StatusText.Text = "Вычисления успешно завершены!";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Вычисления отменены пользователем. Рассчитанная часть кэширована в БД.";
        }
        finally
        {
            // Независимо от результата - отрисовываем то, что успели посчитать, и сбрасываем UI
            _lastExecutedTasks = selectedTasks;
            _lastMatrixResults = matrixResults ?? new List<MatrixBenchmarkResult>();
            CurrentLoadedSessions.Clear();

            RenderTaskCharts(selectedTasks);
            if (_lastMatrixResults.Any())
            {
                RenderMatrixPanel(_lastMatrixResults);
            }
            

            _historyWindow?.LoadHistoryFromDb();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            RunButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
            ProgressPanel.IsVisible = false;
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            CancelButton.IsEnabled = false;
            StatusText.Text = "Остановка вычислений и сохранение кэша...";
            _cancellationTokenSource.Cancel();
        }
    }

    private void RenderTaskCharts(List<BenchmarkTask> tasks)
    {
        if (CombinedChartCheckBox.IsChecked == true)
            RenderCombinedCharts(tasks);
        else
            RenderIndividualCharts(tasks);
    }

    /// <summary>
    /// Draws all selected algorithms on a shared axis. Time-based and step-based
    /// algorithms are measured in different units, so each group gets its own chart.
    /// </summary>
    private void RenderCombinedCharts(List<BenchmarkTask> tasks)
    {
        PlotsPanel.Children.Clear();
        MatrixPanelHost.Children.Clear();
        UpdateEmptyState();
        _activePlots.Clear();

        bool showApprox = ShowApproxCheckBox.IsChecked ?? true;

        foreach (var group in tasks.Where(t => t.Results.Any()).GroupBy(t => t.IsStepMeasurement))
        {
            bool isStep = group.Key;
            var groupTasks = group.ToList();

            var border = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch, Height = 480, Margin = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(8),
                Background = ThemeColors.Brush(ThemeColors.PanelBackground),
                BorderBrush = ThemeColors.Brush(ThemeColors.Border),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(12)
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, 220") };

            var plotControl = new AvaPlot
                { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            ApplyPlotTheme(plotControl);

            Grid.SetColumn(plotControl, 0);
            grid.Children.Add(plotControl);

            var legendScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var legendPanel = new StackPanel
            {
                Spacing = 5, VerticalAlignment = VerticalAlignment.Top, Margin = new Avalonia.Thickness(10, 10, 5, 5)
            };
            legendScroll.Content = legendPanel;

            Grid.SetColumn(legendScroll, 1);
            grid.Children.Add(legendScroll);

            legendPanel.Children.Add(new TextBlock
            {
                Text = "ЛЕГЕНДА", FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = ThemeColors.Brush(ThemeColors.TextMuted),
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            });

            var seriesData = new List<(double[], double[], string)>();
            var unit = PickDisplayUnit(isStep, groupTasks.SelectMany(t => t.Results).Select(r => r.TimeMs));

            for (int i = 0; i < groupTasks.Count; i++)
            {
                var task = groupTasks[i];
                string colorHex = ThemeColors.Series[i % ThemeColors.Series.Length];

                double[] xs = task.Results.Select(r => (double)r.N).ToArray();
                double[] ys = ToDisplayUnit(task.Results.Select(r => r.TimeMs).ToArray(), unit);

                var scatter = plotControl.Plot.Add.Scatter(xs, ys);
                scatter.LineWidth = 2;
                scatter.MarkerSize = 5;
                scatter.MarkerShape = SeriesMarkers[i % SeriesMarkers.Length];
                scatter.Color = ScottPlot.Color.FromHex(colorHex);

                legendPanel.Children.Add(CreateCustomLegendItem(plotControl, scatter, task.Name, colorHex));
                seriesData.Add((xs, ys, task.Name));

                if (showApprox)
                {
                    // Same color as the experiment, dashed, so the pair is easy to match by eye
                    var (_, mse, yApprox) = FitApproximation(xs, ys, task.Name);
                    var approxScatter = plotControl.Plot.Add.Scatter(xs, yApprox);
                    approxScatter.LineWidth = 1.5f;
                    approxScatter.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                    approxScatter.Color = ScottPlot.Color.FromHex(colorHex);
                    approxScatter.MarkerSize = 0;

                    legendPanel.Children.Add(CreateCustomLegendItem(plotControl, approxScatter,
                        $"{task.Name}: теория (MSE: {mse:E1}{MseUnitSuffix(unit)})", colorHex));
                    seriesData.Add((xs, yApprox, $"{task.Name}: теория"));
                }
            }

            AttachHoverTooltip(plotControl, seriesData, ValueUnitText(unit));

            plotControl.Plot.Title(isStep ? "Сравнение алгоритмов (шаги)" : "Сравнение алгоритмов (время)");
            plotControl.Plot.XLabel("Размер массива (N)");
            plotControl.Plot.YLabel(YAxisLabel(unit));
            plotControl.Plot.Axes.AutoScale();
            plotControl.Refresh();

            border.Child = WrapWithZoomToolbar(grid, plotControl);
            AddCard(PlotsPanel, border);
            _activePlots.Add(plotControl);
        }
    }

    private void RenderIndividualCharts(List<BenchmarkTask> tasks)
    {
        PlotsPanel.Children.Clear();
        MatrixPanelHost.Children.Clear();
        UpdateEmptyState();
        _activePlots.Clear();

        bool showApprox = ShowApproxCheckBox.IsChecked ?? true;

        foreach (var task in tasks)
        {
            var border = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch, Height = 360, Margin = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(8),
                Background = ThemeColors.Brush(ThemeColors.PanelBackground),
                BorderBrush = ThemeColors.Brush(ThemeColors.Border),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(12)
            };

            // Разделяем область на график и панель легенды справа
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, 180") };

            var plotControl = new AvaPlot
                { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

            ApplyPlotTheme(plotControl);

            Grid.SetColumn(plotControl, 0);
            grid.Children.Add(plotControl);

            // Контейнер легенды (с прокруткой, если элементов слишком много)
            var legendScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            var legendPanel = new StackPanel
            {
                Spacing = 5, VerticalAlignment = VerticalAlignment.Top, Margin = new Avalonia.Thickness(10, 10, 5, 5)
            };
            legendScroll.Content = legendPanel;

            Grid.SetColumn(legendScroll, 1);
            grid.Children.Add(legendScroll);

            var titleText = new TextBlock
            {
                Text = "ЛЕГЕНДА", FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = ThemeColors.Brush(ThemeColors.TextMuted),
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            };
            legendPanel.Children.Add(titleText);

            double[] xs = task.Results.Select(r => (double)r.N).ToArray();
            var unit = PickDisplayUnit(task.IsStepMeasurement, task.Results.Select(r => r.TimeMs));
            double[] ys = ToDisplayUnit(task.Results.Select(r => r.TimeMs).ToArray(), unit);

            if (xs.Length > 0 && ys.Length > 0)
            {
                var empiricalScatter = plotControl.Plot.Add.Scatter(xs, ys);
                empiricalScatter.LineWidth = 2;
                empiricalScatter.MarkerSize = 4;
                empiricalScatter.Color = ThemeColors.Plot(ThemeColors.Accent);

                legendPanel.Children.Add(
                    CreateCustomLegendItem(plotControl, empiricalScatter, "Эксперимент", ThemeColors.Accent));

                if (showApprox)
                {
                    var (c, mse, yApprox) = FitApproximation(xs, ys, task.Name);
                    var approxScatter = plotControl.Plot.Add.Scatter(xs, yApprox);
                    approxScatter.LineWidth = 2;
                    approxScatter.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                    approxScatter.Color = ThemeColors.Plot(ThemeColors.Theory);
                    approxScatter.MarkerSize = 0;

                    legendPanel.Children.Add(CreateCustomLegendItem(plotControl, approxScatter,
                        $"Теория (MSE: {mse:E2}{MseUnitSuffix(unit)})", ThemeColors.Theory));
                }

                var seriesData = new List<(double[], double[], string)> { (xs, ys, "Эксперимент") };
                if (showApprox)
                {
                    var (_, _, yApprox) = FitApproximation(xs, ys, task.Name);
                    seriesData.Add((xs, yApprox, "Теория"));
                }

                AttachHoverTooltip(plotControl, seriesData, ValueUnitText(unit));

                plotControl.Plot.Axes.AutoScale();
            }

            plotControl.Plot.Title(task.Name);
            plotControl.Plot.XLabel("Размер массива (N)");
            plotControl.Plot.YLabel(YAxisLabel(unit));
            plotControl.Refresh();

            border.Child = WrapWithZoomToolbar(grid, plotControl);
            AddCard(PlotsPanel, border);
            _activePlots.Add(plotControl);
        }
        
    }


    private Control CreateCustomLegendItem(AvaPlot plotControl, ScottPlot.Plottables.Scatter scatter, string name,
        string colorHex)
    {
        var panel = new StackPanel
            { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Avalonia.Thickness(0, 4), VerticalAlignment = VerticalAlignment.Center };

        // Кнопка-глазик: иконка показывает, видна ли серия
        var eyeIcon = new Avalonia.Controls.Shapes.Path { Data = (Geometry)FindIcon("IconEye") };
        var eyeButton = new Avalonia.Controls.Primitives.ToggleButton
        {
            IsChecked = true, // По умолчанию график отображается
            Content = eyeIcon,
            VerticalAlignment = VerticalAlignment.Center
        };
        eyeButton.Classes.Add("icon-toggle");
        ToolTip.SetTip(eyeButton, "Показать или скрыть серию");

        // Образец линии: цвет + стиль (сплошная/пунктир) + маркер, чтобы серии различались не только цветом
        var swatch = CreateLegendSwatch(scatter, colorHex);

        eyeButton.IsCheckedChanged += (_, _) =>
        {
            bool isVisible = eyeButton.IsChecked ?? false;
            scatter.IsVisible = isVisible;
            eyeIcon.Data = (Geometry)FindIcon(isVisible ? "IconEye" : "IconEyeOff");
            swatch.Opacity = isVisible ? 1.0 : 0.4;
            plotControl.Refresh();
        };

        // Текст (Название алгоритма / MSE аппроксимации)
        var textBlock = new TextBlock
        {
            Text = name,
            Foreground = ThemeColors.Brush(ThemeColors.Text),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontSize = 12,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 130
        };

        eyeButton.IsCheckedChanged += (_, _) => textBlock.Opacity = eyeButton.IsChecked == true ? 1.0 : 0.55;

        panel.Children.Add(eyeButton);
        panel.Children.Add(swatch);
        panel.Children.Add(textBlock);

        return panel;
    }

    private static object FindIcon(string key) =>
        Avalonia.Application.Current!.FindResource(key) ?? throw new InvalidOperationException($"Icon '{key}' not found");

    private static Control CreateLegendSwatch(ScottPlot.Plottables.Scatter scatter, string colorHex)
    {
        var brush = ThemeColors.Brush(colorHex);
        var canvas = new Canvas { Width = 20, Height = 14, VerticalAlignment = VerticalAlignment.Center };

        var line = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(0, 7),
            EndPoint = new Avalonia.Point(20, 7),
            Stroke = brush,
            StrokeThickness = 2
        };
        if (scatter.LineStyle.Pattern.Equals(ScottPlot.LinePattern.Dashed))
            line.StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 2, 1.5 };
        canvas.Children.Add(line);

        if (scatter.MarkerSize > 0)
        {
            // Same shape as on the chart, centered on the line (7x7 box at x=6.5, y=3.5)
            var (geometry, filled) = scatter.MarkerShape switch
            {
                ScottPlot.MarkerShape.FilledSquare => ("M0,0 H7 V7 H0 Z", true),
                ScottPlot.MarkerShape.OpenSquare => ("M0,0 H7 V7 H0 Z", false),
                ScottPlot.MarkerShape.FilledTriangleUp => ("M3.5,0 L7,7 H0 Z", true),
                ScottPlot.MarkerShape.FilledTriangleDown => ("M0,0 H7 L3.5,7 Z", true),
                ScottPlot.MarkerShape.FilledDiamond => ("M3.5,0 L7,3.5 L3.5,7 L0,3.5 Z", true),
                ScottPlot.MarkerShape.OpenDiamond => ("M3.5,0 L7,3.5 L3.5,7 L0,3.5 Z", false),
                ScottPlot.MarkerShape.OpenCircle => ("M0,3.5 A3.5,3.5 0 1 1 7,3.5 A3.5,3.5 0 1 1 0,3.5 Z", false),
                _ => ("M0,3.5 A3.5,3.5 0 1 1 7,3.5 A3.5,3.5 0 1 1 0,3.5 Z", true)
            };

            var marker = new Avalonia.Controls.Shapes.Path
            {
                Data = Geometry.Parse(geometry),
                Fill = filled ? brush : ThemeColors.Brush(ThemeColors.PanelBackground),
                Stroke = brush,
                StrokeThickness = filled ? 0 : 1.5
            };
            Canvas.SetLeft(marker, 6.5);
            Canvas.SetTop(marker, 3.5);
            canvas.Children.Add(marker);
        }

        return canvas;
    }

    /// <summary>
    ///При переключении галочки перерисовываем актуальные графики
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ShowApprox_Click(object? sender, RoutedEventArgs e) => RerenderCharts();

    private void RerenderCharts()
    {
        if (CurrentLoadedSessions.Any())
        {
            RenderComparisonCharts(CurrentLoadedSessions);
        }
        else if (_lastExecutedTasks.Any() || _lastMatrixResults.Any())
        {
            RenderTaskCharts(_lastExecutedTasks);
            if (_lastMatrixResults.Any()) RenderMatrixPanel(_lastMatrixResults);
        }
    }

    private void SelectAll_Click(object? sender, RoutedEventArgs e) =>
        AlgorithmItems.ToList().ForEach(i => i.IsSelected = true);

    private void DeselectAll_Click(object? sender, RoutedEventArgs e) =>
        AlgorithmItems.ToList().ForEach(i => i.IsSelected = false);

    // ===== Панель масштаба отдельно для каждой карточки =====

    private static Button CreateToolButton(string text, Action onClick)
    {
        var button = new Button { Content = text };
        button.Classes.Add("tool");
        button.Click += (_, _) => onClick();
        return button;
    }

    private static Control CreateZoomToolbar(
        IEnumerable<(string Label, Action ZoomIn, Action ZoomOut)> axes, Action reset)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 6)
        };

        foreach (var (label, zoomIn, zoomOut) in axes)
        {
            var text = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(6, 0, 0, 0)
            };
            text.Classes.Add("muted");
            panel.Children.Add(text);
            // Icon-like "+"/"−" buttons get a tooltip naming the action
            string axisName = label.TrimEnd(':');
            var zoomInButton = CreateToolButton("+", zoomIn);
            var zoomOutButton = CreateToolButton("−", zoomOut);
            ToolTip.SetTip(zoomInButton, $"Увеличить ({axisName})");
            ToolTip.SetTip(zoomOutButton, $"Уменьшить ({axisName})");
            panel.Children.Add(zoomInButton);
            panel.Children.Add(zoomOutButton);
        }

        var resetButton = new Button
        {
            Content = "Сбросить масштаб",
            Height = 32,
            Margin = new Avalonia.Thickness(6, 0, 0, 0)
        };
        resetButton.Click += (_, _) => reset();
        panel.Children.Add(resetButton);

        return panel;
    }

    /// <summary>
    /// Оборачивает содержимое 2D-карточки: сверху её собственная панель масштаба.
    /// </summary>
    private static Control WrapWithZoomToolbar(Control content, AvaPlot plot)
    {
        void Zoom(double fx, double fy)
        {
            plot.Plot.Axes.Zoom(fx, fy);
            plot.Refresh();
        }

        var toolbar = CreateZoomToolbar(
            new (string, Action, Action)[]
            {
                ("Ось X:", () => Zoom(1.2, 1.0), () => Zoom(0.8, 1.0)),
                ("Ось Y:", () => Zoom(1.0, 1.2), () => Zoom(1.0, 0.8))
            },
            () =>
            {
                plot.Plot.Axes.AutoScale();
                plot.Refresh();
            });

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        Grid.SetRow(toolbar, 0);
        Grid.SetRow(content, 1);
        root.Children.Add(toolbar);
        root.Children.Add(content);
        return root;
    }

    /// <summary>
    /// Шапка карточки матрицы: заголовок слева, панель масштаба справа.
    /// </summary>
    private static Control BuildMatrixHeader(string label, MatrixSurfaceControl control)
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Avalonia.Thickness(4, 0, 0, 0)
        };

        var title = new TextBlock
        {
            Text = label,
            Foreground = ThemeColors.Brush(ThemeColors.Text),
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var toolbar = CreateZoomToolbar(
            new (string, Action, Action)[]
            {
                ("Масштаб:", () => control.ZoomBy(1.2), () => control.ZoomBy(1 / 1.2))
            },
            () => control.ResetView());

        Grid.SetColumn(title, 0);
        Grid.SetColumn(toolbar, 1);
        header.Children.Add(title);
        header.Children.Add(toolbar);
        return header;
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
            // Если окно было свернуто, разворачиваем его обратно
            if (_historyWindow.WindowState == WindowState.Minimized)
            {
                _historyWindow.WindowState = WindowState.Normal;
            }

            // Выводим окно поверх остальных и передаем ему фокус
            _historyWindow.Activate();
        }
    }

    public void RenderComparisonCharts(List<HistorySession> sessionsToCompare)
    {
        PlotsPanel.Children.Clear();
        MatrixPanelHost.Children.Clear();
        UpdateEmptyState();
        _activePlots.Clear();

        if (sessionsToCompare == null || !sessionsToCompare.Any())
        {
            return;
        }

        bool showApprox = ShowApproxCheckBox.IsChecked ?? true;
        var allResults = sessionsToCompare.SelectMany(s => (IEnumerable<ExperimentResult>)s.Results).ToList();
        var uniqueAlgorithms = allResults.Select(r => r.AlgorithmName).Distinct().ToList();

        var colors = ThemeColors.Series;

        foreach (var algoName in uniqueAlgorithms)
        {
            var algoAllResults = allResults.Where(r => r.AlgorithmName == algoName).ToList();
            bool isMatrixAlgo = algoAllResults.Any(r => r.M.HasValue);

            if (isMatrixAlgo)
            {
                var seriesList = new List<MatrixSeries>();

                for (int i = 0; i < sessionsToCompare.Count; i++)
                {
                    var session = sessionsToCompare[i];
                    var sessionMatrixResults = session.Results
                        .Where(r => r.AlgorithmName == algoName && r.M.HasValue)
                        .Select(r => new MatrixBenchmarkResult(r.N, r.M!.Value, r.ElapsedTimeMs))
                        .ToList();

                    if (sessionMatrixResults.Any())
                    {
                        var colorHex = colors[i % colors.Length];
                        var color = Color.Parse(colorHex);
                        seriesList.Add(new MatrixSeries
                        {
                            Name = $"№{i + 1} ({session.Date:g})",
                            Results = sessionMatrixResults,
                            Color = color
                        });
                    }
                }

                if (showApprox && seriesList.Any())
                {
                    var displaySeriesList = new List<MatrixSeries>();
                    for (int i = 0; i < seriesList.Count; i++)
                    {
                        var s = seriesList[i];
                        displaySeriesList.Add(s);

                        var (c, mse) = FitMatrixApproximation(s.Results);
                        var approxResults = s.Results
                            .Select(r =>
                                new MatrixBenchmarkResult(r.N, r.M, c * GetMatrixTheoreticalComplexity(r.N, r.M)))
                            .ToList();

                        string approxName = seriesList.Count > 1
                            ? $"Теория №{i + 1} (MSE: {mse:E1})"
                            : $"Теория (MSE: {mse:E1})";

                        displaySeriesList.Add(new MatrixSeries
                        {
                            Name = approxName,
                            Results = approxResults,
                            // Устанавливаем значение альфа-канала 128 (50% прозрачности) для базового цвета
                            Color = seriesList.Count > 1
                                ? Color.FromArgb(64, s.Color.R, s.Color.G, s.Color.B)
                                : Color.Parse(ThemeColors.Theory)
                        });
                    }

                    RenderMatrixPanelForComparison(displaySeriesList, $"{algoName} — Сравнение сессий");
                }
                else if (seriesList.Count > 1)
                {
                    RenderMatrixPanelForComparison(seriesList, $"{algoName} — Сравнение сессий");
                }
                else if (seriesList.Count == 1)
                {
                    RenderMatrixPanel(seriesList[0].Results, $"{algoName} — {seriesList[0].Name}");
                }

                continue;
            }

            var border = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch, Height = 360, Margin = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(8),
                Background = ThemeColors.Brush(ThemeColors.PanelBackground),
                BorderBrush = ThemeColors.Brush(ThemeColors.Border),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(12)
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, 180") };

            var plotControl = new AvaPlot
            {
                HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
            };
            ApplyPlotTheme(plotControl);

            Grid.SetColumn(plotControl, 0);
            grid.Children.Add(plotControl);

            var legendScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            var legendPanel = new StackPanel
            {
                Spacing = 5, VerticalAlignment = VerticalAlignment.Top, Margin = new Avalonia.Thickness(10, 10, 5, 5)
            };
            legendScroll.Content = legendPanel;

            Grid.SetColumn(legendScroll, 1);
            grid.Children.Add(legendScroll);

            var titleText = new TextBlock
            {
                Text = "ЛЕГЕНДА", FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = ThemeColors.Brush(ThemeColors.TextMuted),
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            };
            legendPanel.Children.Add(titleText);

            var sessionsWithAlgo =
                sessionsToCompare.Where(s => s.Results.Any(r => r.AlgorithmName == algoName)).ToList();
            bool isShared = sessionsWithAlgo.Count > 1;

            var seriesData = new List<(double[], double[], string)>();

            bool isStepMeasurement = AlgorithmItems.FirstOrDefault(a => a.Name == algoName)?.Task?.IsStepMeasurement ?? false;
            // One unit for all sessions so they stay comparable on the same axis
            var unit = PickDisplayUnit(isStepMeasurement, algoAllResults.Select(r => r.ElapsedTimeMs));

            for (int i = 0; i < sessionsToCompare.Count; i++)
            {
                var session = sessionsToCompare[i];
                var sessionAlgoResults = session.Results
                    .Where(r => r.AlgorithmName == algoName)
                    .OrderBy(r => r.N)
                    .ToList();

                if (!sessionAlgoResults.Any()) continue;

                double[] xs = sessionAlgoResults.Select(r => (double)r.N).ToArray();
                // Step-based algorithms store their measurement in StepCount, not in ElapsedTimeMs
                double[] ys = isStepMeasurement
                    ? sessionAlgoResults.Select(r => (double)(r.StepCount ?? 0)).ToArray()
                    : ToDisplayUnit(sessionAlgoResults.Select(r => r.ElapsedTimeMs).ToArray(), unit);

                var scatter = plotControl.Plot.Add.Scatter(xs, ys);
                scatter.LineWidth = 2;
                scatter.MarkerSize = isShared ? 5 : 4;
                if (isShared) scatter.MarkerShape = SeriesMarkers[i % SeriesMarkers.Length];

                string colorHex = isShared ? colors[i % colors.Length] : ThemeColors.Accent;
                scatter.Color = ScottPlot.Color.FromHex(colorHex);

                string legendText = isShared ? $"№ {i + 1}" : "Эксперимент";
                legendPanel.Children.Add(CreateCustomLegendItem(plotControl, scatter, legendText, colorHex));
                seriesData.Add((xs, ys, legendText));

                if (showApprox && xs.Length > 0)
                {
                    var (c, mse, yApprox) = FitApproximation(xs, ys, algoName);
                    var approxScatter = plotControl.Plot.Add.Scatter(xs, yApprox);
                    approxScatter.LineWidth = 1.5f;
                    approxScatter.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;

                    string approxColorHex = isShared ? colors[i % colors.Length] : ThemeColors.Theory;
                    approxScatter.Color = ScottPlot.Color.FromHex(approxColorHex);
                    approxScatter.MarkerSize = 0;

                    string mseText = $"MSE: {mse:E1}{MseUnitSuffix(unit)}";
                    string approxLegendText = isShared ? $"Теория №{i + 1} ({mseText})" : $"Теория ({mseText})";
                    legendPanel.Children.Add(CreateCustomLegendItem(plotControl, approxScatter, approxLegendText,
                        approxColorHex));
                    seriesData.Add((xs, yApprox, isShared ? $"Теория №{i + 1}" : "Теория"));
                }
            }

            AttachHoverTooltip(plotControl, seriesData, ValueUnitText(unit));

            plotControl.Plot.Title(algoName, size: null);
            plotControl.Plot.XLabel("Размер массива (N)");

            plotControl.Plot.YLabel(YAxisLabel(unit));

            plotControl.Plot.Axes.AutoScale();
            plotControl.Refresh();

            border.Child = WrapWithZoomToolbar(grid, plotControl);
            AddCard(PlotsPanel, border);
            _activePlots.Add(plotControl);
        }

        StatusText.Text = $"Отображено данных на графиках: {sessionsToCompare.Count} сессий";
    }

    private void RenderMatrixPanelForComparison(List<MatrixSeries> seriesList,
        string label = "Умножение матриц — Сравнение сессий")
    {
        if (seriesList.Count == 0) return;

        var border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 550,
            Margin = new Avalonia.Thickness(0),
            Padding = new Avalonia.Thickness(8),
            Background = ThemeColors.Brush(ThemeColors.PanelBackground),
            BorderBrush = ThemeColors.Brush(ThemeColors.Border),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(12)
        };

        var control = new MatrixSurfaceControl
        {
            Height = 480,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(BuildMatrixHeader(label, control));
        stack.Children.Add(control);

        border.Child = stack;
        AddCard(MatrixPanelHost, border);
        control.SetMultipleResults(seriesList);
    }

    // ===== Единицы измерения на графиках =====
    // Step-based tasks keep their raw step counts, so their unit is null.

    private static TimeUnit? PickDisplayUnit(bool isStepMeasurement, IEnumerable<double> valuesMs) =>
        isStepMeasurement ? null : TimeUnit.Pick(valuesMs.DefaultIfEmpty(0).Max());

    private static double[] ToDisplayUnit(double[] valuesMs, TimeUnit? unit) =>
        unit?.FromMs(valuesMs) ?? valuesMs;

    private static string YAxisLabel(TimeUnit? unit) =>
        unit is null ? "Количество операций (шаги)" : $"Время ({unit.Symbol})";

    private static string ValueUnitText(TimeUnit? unit) => unit?.Symbol ?? "шагов";

    // MSE is computed from the displayed values, so it is measured in the squared display unit
    private static string MseUnitSuffix(TimeUnit? unit) => unit is null ? "" : $" {unit.Symbol}²";

    private void AttachHoverTooltip(AvaPlot plotControl, List<(double[] xs, double[] ys, string name)> dataSeries, string unit = "мс")
    {
        ToolTip.SetShowDelay(plotControl, 0);

        // Ring around the hovered point, so it is clear which point the tooltip describes.
        // Created on first hover: an extra plottable added earlier would affect auto-scaling.
        ScottPlot.Plottables.Marker? highlight = null;

        plotControl.PointerMoved += (sender, e) =>
        {
            // 1. Получаем координаты мыши и переводим в систему координат данных графика
            var position = e.GetPosition(plotControl);
            var mousePixel = new ScottPlot.Pixel((float)position.X, (float)position.Y);
            var mouseCoords = plotControl.Plot.GetCoordinates(mousePixel);

            // 2. Получаем текущие видимые границы осей для нормализации масштаба
            var limits = plotControl.Plot.Axes.GetLimits();
            double xRange = limits.Right - limits.Left;
            double yRange = limits.Top - limits.Bottom;

            if (xRange <= 0 || yRange <= 0) return;

            double minDistance = double.MaxValue;
            double closestX = 0;
            double closestY = 0;
            string closestName = "";
            bool found = false;

            // 3. Ищем ближайшую точку без тяжелых пиксельных конвертаций
            foreach (var series in dataSeries)
            {
                for (int i = 0; i < series.xs.Length; i++)
                {
                    // Нормализуем разницу от 0 до 1 относительно текущего зума графика
                    double dx = (series.xs[i] - mouseCoords.X) / xRange;
                    double dy = (series.ys[i] - mouseCoords.Y) / yRange;

                    // Квадрат расстояния в нормализованных координатах
                    double dist = dx * dx + dy * dy;

                    // Порог прилипания ~0.001 (соответствует радиусу около 3% от размера окна)
                    if (dist < 0.001 && dist < minDistance)
                    {
                        minDistance = dist;
                        closestX = series.xs[i];
                        closestY = series.ys[i];
                        closestName = series.name;
                        found = true;
                    }
                }
            }

            // 4. Управляем нативной всплывающей подсказкой Avalonia
            if (found)
            {
                string text = string.IsNullOrEmpty(closestName)
                    ? $"N: {closestX}\nЗначение: {closestY:F3} {unit}"
                    : $"{closestName}\nN: {closestX}\nЗначение: {closestY:F3} {unit}";

                ToolTip.SetTip(plotControl, text);
                ToolTip.SetIsOpen(plotControl, true);
                ShowHighlight(closestX, closestY);
            }
            else
            {
                ToolTip.SetIsOpen(plotControl, false);
                HideHighlight();
            }
        };

        plotControl.PointerExited += (sender, e) =>
        {
            ToolTip.SetIsOpen(plotControl, false);
            HideHighlight();
        };

        void ShowHighlight(double x, double y)
        {
            if (highlight is null)
            {
                highlight = plotControl.Plot.Add.Marker(x, y, ScottPlot.MarkerShape.OpenCircle, 14,
                    ThemeColors.Plot(ThemeColors.Text));
                highlight.MarkerLineWidth = 2;
            }
            else if (highlight.IsVisible && highlight.X == x && highlight.Y == y)
            {
                return; // same point: no redraw
            }

            highlight.Location = new ScottPlot.Coordinates(x, y);
            highlight.IsVisible = true;
            plotControl.Refresh();
        }

        void HideHighlight()
        {
            if (highlight is not { IsVisible: true }) return;
            highlight.IsVisible = false;
            plotControl.Refresh();
        }
    }
}