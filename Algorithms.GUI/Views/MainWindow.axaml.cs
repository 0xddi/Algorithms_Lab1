using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Algorithms.Core;
using Algorithms.Core.MathFunctions;
using Algorithms.Core.PolynomialAlgorithms;
using Algorithms.Core.PowFunctionAlgorithms;

namespace Algorithms.GUI.Views;

public partial class MainWindow : Window
{
    private Benchmarker _benchmarker;

    public MainWindow()
    {
        InitializeComponent();
        InitializeBenchmarker();
    }

    private void InitializeBenchmarker()
    {
        double[] numbers = new double[2000];
        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] = Random.Shared.NextDouble() * 100.0;
        }

        _benchmarker = new Benchmarker(numbers);

        // ==========================================
        // 1. Сортировки (SortingAlgorithms)
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Bubble Sort", slice => new BubbleSortAlgorithm(slice).RunBench(5)));
        _benchmarker.AddTask(new BenchmarkTask("Quick Sort", slice => new QuickSortAlgorithm(slice).RunBench(5)));
        _benchmarker.AddTask(new BenchmarkTask("Tim Sort", slice => new TimSortAlgorithm(slice).RunBench(5)));

        // ==========================================
        // 2. Математические функции над вектором (MathFunctionAlgorithms)
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Constant Function f(v)=1", slice => new ConstantFunctionAlgorithm(slice).RunBench(5)));
        _benchmarker.AddTask(new BenchmarkTask("Sum Algorithm", slice => new SumAlgorithm(slice).RunBench(5)));
        _benchmarker.AddTask(new BenchmarkTask("Product Algorithm", slice => new ProductAlgorithm(slice).RunBench(5)));

        // ==========================================
        // 3. Вычисление полиномов при x = 1.5 (PolynomialAlgorithms)
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Naive Polynomial", slice => new NaivePolynomialAlgorithm(slice).RunBench(5)));
        _benchmarker.AddTask(new BenchmarkTask("Horner Polynomial", slice => new HornerPolynomialAlgorithm(slice).RunBench(5)));

        // ==========================================
        // 4. Возведение в степень x^n (PowFunctionAlgorithms)
        // ==========================================
        const double baseX = 1.5;

        _benchmarker.AddTask(new BenchmarkTask("Simple Pow (x^n)", 
            slice => new SimplePowAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));

        _benchmarker.AddTask(new BenchmarkTask("Recursive Pow", 
            slice => new RecursivePowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));

        _benchmarker.AddTask(new BenchmarkTask("Fast Pow", 
            slice => new FastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));

        _benchmarker.AddTask(new BenchmarkTask("Classic Fast Pow", 
            slice => new ClassicFastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));
    }

    private async void RunButton_Click(object? sender, RoutedEventArgs e)
    {
        RunButton.IsEnabled = false;
        ProgressIndicator.IsVisible = true;
        StatusText.Text = "Выполняются замеры. Пожалуйста, подождите...";
        
        AvaPlot1.Plot.Clear();

        await Task.Run(() => 
        {
            _benchmarker.Run(benchCycles: 5);
        });

        RenderCharts();

        RunButton.IsEnabled = true;
        ProgressIndicator.IsVisible = false;
        StatusText.Text = "Вычисления завершены!";
    }

    private void RenderCharts()
    {
        foreach (var task in _benchmarker.Tasks)
        {
            double[] xs = task.Results.Select(r => (double)r.N).ToArray();
            double[] ys = task.Results.Select(r => r.TimeMs).ToArray();

            var scatter = AvaPlot1.Plot.Add.ScatterLine(xs, ys);
            scatter.LegendText = task.Name;
            scatter.LineWidth = 2;
        }

        AvaPlot1.Plot.Title("Асимптотическая сложность алгоритмов");
        AvaPlot1.Plot.XLabel("Размер массива (N)");
        AvaPlot1.Plot.YLabel("Время выполнения (мс)");
        AvaPlot1.Plot.ShowLegend();

        AvaPlot1.Plot.Axes.AutoScale();
        AvaPlot1.Refresh();
    }
}