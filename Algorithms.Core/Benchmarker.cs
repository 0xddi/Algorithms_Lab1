using System;
using System.Collections.Generic;

namespace Algorithms.Core;

public class Benchmarker
{
    private readonly double[] _masterData;
    
    // Список всех алгоритмов, которые нужно протестировать
    public List<BenchmarkTask> Tasks { get; } = new List<BenchmarkTask>();

    public Benchmarker(double[] masterData)
    {
        _masterData = masterData;
    }

    public void AddTask(BenchmarkTask task)
    {
        Tasks.Add(task);
    }

    public void Run(int benchCycles = 5)
    {
        // Запускаем каждый алгоритм по очереди
        foreach (var task in Tasks)
        {
            // Жесткая очистка мусора перед тестированием нового алгоритма
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            for (int i = 0; i < _masterData.Length; i++)
            {
                int currentN = i + 1;
                var currentDataSlice = _masterData[0..currentN];

                // Вся магия типов спрятана внутри делегата. Бенчмаркер не знает, 
                // какой алгоритм вызывается, он просто передает срез и получает время.
                double avgTimeMs = task.RunMeasurement(currentDataSlice); 
                double avgTicks = avgTimeMs * TimeSpan.TicksPerMillisecond;

                task.Results.Add((currentN, avgTimeMs, avgTicks));
            }
        }
    }

    public void DebugPrintResults()
    {
        foreach (var task in Tasks)
        {
            Console.WriteLine($"\n=== Результаты для: {task.Name} ===");
            Console.WriteLine("N;TimeMs;Ticks");
            
            foreach (var result in task.Results)
            {
                Console.WriteLine($"{result.N};{result.TimeMs:F6};{result.TimeTicks:F2}");
            }
        }
    }
}