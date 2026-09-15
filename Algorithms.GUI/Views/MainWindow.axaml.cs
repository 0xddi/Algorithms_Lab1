using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Algorithms.Core;
using Algorithms.Core.MathFunctions;
using Algorithms.Core.PolynomialAlgorithms;
using Algorithms.Core.PowFunctionAlgorithms;
using Avalonia; 

namespace Algorithms.GUI.Views;

public partial class MainWindow : Window
{
    private Benchmarker _benchmarker;

    public MainWindow()
    {
        InitializeComponent();
        InitializeBenchmarker();
    }

    /// <summary>
    /// Возвращает набор чисел от start до end с постоянным шагом step.
    /// </summary>
    private static int[] Range(int start, int end, int step)
    {
        var list = new List<int>();
        for (int v = start; v <= end; v += step)
            list.Add(v);
        return list.ToArray();
    }

    private void InitializeBenchmarker()
    {
        // Мастер-массив — 500 000 хватит для всех сеток ниже.
        double[] numbers = new double[500_000];
        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] = Random.Shared.NextDouble() * 100.0;
        }

        _benchmarker = new Benchmarker(numbers);

        // ==========================================
        // Сетки — с постоянным шагом, чтобы линии были гладкими
        // ==========================================

        int[] quadraticSizes = Range(1, 2_000, 25);
        int[] linearithmicSizes = Range(1, 20_000, 250);
        int[] linearSizes = Range(1, 500_000, 1_000);
        int[] logSizes = Range(1, 500_000, 1_000);
        int[] tinySizes = Range(1, 2_000, 25);

        const double baseX = 1.5;
        const int cycles = 3;

        // ==========================================
        // 1. Сортировки
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Bubble Sort",
            slice => new BubbleSortAlgorithm(slice).RunBench(cycles),
            quadraticSizes));

        _benchmarker.AddTask(new BenchmarkTask("Quick Sort",
            slice => new QuickSortAlgorithm(slice).RunBench(cycles),
            linearithmicSizes));

        _benchmarker.AddTask(new BenchmarkTask("Tim Sort",
            slice => new TimSortAlgorithm(slice).RunBench(cycles),
            linearithmicSizes));

        // ==========================================
        // 2. Матфункции
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Constant Function f(v)=1",
            slice => new ConstantFunctionAlgorithm(slice).RunBench(cycles),
            linearSizes));

        _benchmarker.AddTask(new BenchmarkTask("Sum Algorithm",
            slice => new SumAlgorithm(slice).RunBench(cycles),
            linearSizes));

        _benchmarker.AddTask(new BenchmarkTask("Product Algorithm",
            slice => new ProductAlgorithm(slice).RunBench(cycles),
            linearSizes));

        // ==========================================
        // 3. Полиномы
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Naive Polynomial",
            slice => new NaivePolynomialAlgorithm(slice).RunBench(cycles),
            tinySizes));

        _benchmarker.AddTask(new BenchmarkTask("Horner Polynomial",
            slice => new HornerPolynomialAlgorithm(slice).RunBench(cycles),
            linearSizes));

        // ==========================================
        // 4. Возведение в степень
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Simple Pow (x^n)",
            slice => new SimplePowAlgorithm((x: baseX, n: slice.Length)).RunBench(cycles),
            tinySizes));

        _benchmarker.AddTask(new BenchmarkTask("Recursive Pow",
            slice => new RecursivePowerAlgorithm((x: baseX, n: slice.Length)).RunBench(cycles),
            tinySizes));

        _benchmarker.AddTask(new BenchmarkTask("Fast Pow",
            slice => new FastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(cycles),
            logSizes));

        _benchmarker.AddTask(new BenchmarkTask("Classic Fast Pow",
            slice => new ClassicFastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(cycles),
            logSizes));

        // ==========================================
        // 5. Ахо-Корасик
        // ==========================================
        _benchmarker.AddTask(new BenchmarkTask("Aho-Corasick",
            slice => new AhoCorasickAlgorithm(slice).RunBench(cycles),
            linearSizes));
    }

    private async void RunButton_Click(object? sender, RoutedEventArgs e)
    {
        RunButton.IsEnabled = false;
        ProgressIndicator.IsVisible = true;
        StatusText.Text = "Выполняются замеры. Пожалуйста, подождите...";

        await Task.Run(() =>
        {
            _benchmarker.Run(benchCycles: 3);
        });

        OpenChartWindows();

        RunButton.IsEnabled = true;
        ProgressIndicator.IsVisible = false;
        StatusText.Text = "Вычисления завершены! Открыто окон: " + _benchmarker.Tasks.Count;
    }

    /// <summary>
    /// Открывает по одному окну с графиком для каждого алгоритма.
    /// </summary>
    private void OpenChartWindows()
    {
        // Немного смещаем окна, чтобы они не накладывались друг на друга идеально
        int offset = 0;

        foreach (var task in _benchmarker.Tasks)
        {
            if (task.Results.Count == 0)
                continue;

            var window = new ChartWindow(task)
            {
                Position = new PixelPoint(
                    this.Position.X + 40 + offset,
                    this.Position.Y + 40 + offset)
            };

            // Show — окна не модальные, можно смотреть все сразу
            window.Show(this);

            offset += 30;
        }
    }
}