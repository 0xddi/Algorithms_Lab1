using System;
using System.Collections.Generic;

namespace Algorithms.Core;

public class Benchmarker
{
    private readonly double[] _dataRef;

    public List<(int N, double TimeMs, double TimeTicks)> BenchResults { get; } 
        = new List<(int, double, double)>(2000);

    public Benchmarker(double[] dataRef)
    {
        _dataRef = dataRef;
    }

    public void Run()
    {
        // 1. Очистка памяти перед запуском основного цикла
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 2. Основной цикл замеров
        for (int i = 0; i < _dataRef.Length; i++)
        {
            int currentN = i + 1;
            var currentDataSlice = _dataRef[0..currentN];

            var algo = new BubbleSortAlgorithm(currentDataSlice);
            
            // RunBench возвращает точное среднее значение в миллисекундах (double)
            double avgTimeMs = algo.RunBench(5); 

            // 1 миллисекунда = 10 000 системных тиков
            double avgTicks = avgTimeMs * TimeSpan.TicksPerMillisecond;

            BenchResults.Add((currentN, avgTimeMs, avgTicks));
        }
    }

    public void PrintResults()
    {
        // Форматированный вывод: N; Время (мс); Время (тики)
        Console.WriteLine("N;TimeMs;Ticks");
        foreach (var result in BenchResults)
        {
            Console.WriteLine($"{result.N};{result.TimeMs:F6};{result.TimeTicks:F2}");
        }
    }
}