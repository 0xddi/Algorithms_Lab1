using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.Core.Database;

namespace Algorithms.Core;

public class Benchmarker
{
    private readonly double[] _masterData;
    public List<BenchmarkTask> Tasks { get; } = new List<BenchmarkTask>();

    public Benchmarker(double[] masterData)
    {
        _masterData = masterData;
    }

    public void AddTask(BenchmarkTask task) => Tasks.Add(task);

    // Добавлен флаг useCache
    public void RunFiltered(IEnumerable<BenchmarkTask> tasksToRun, bool useCache = true, int benchCycles = 5)
    {
        using var db = new AppDbContext();
        var experimentDate = DateTime.Now;

        foreach (var task in tasksToRun)
        {
            task.Results.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Группируем по N и берем последнее измерение по дате, чтобы избежать дублирования ключей
            var cachedData = useCache 
                ? db.Results
                    .Where(r => r.AlgorithmName == task.Name)
                    .GroupBy(r => r.N)
                    .ToDictionary(
                        g => g.Key, 
                        g => g.OrderByDescending(r => r.ExperimentDate).First().ElapsedTimeMs
                    )
                : new Dictionary<int, double>();

            for (int i = 0; i < _masterData.Length; i++)
            {
                int currentN = i + 1;

                // 1. Механизм кэширования
                if (useCache && cachedData.TryGetValue(currentN, out double cachedTime))
                {
                    double cachedTicks = cachedTime * TimeSpan.TicksPerMillisecond;
                    task.Results.Add((currentN, cachedTime, cachedTicks));
                    continue;
                }

                // 2. Если в кэше нет — запускаем замер
                var currentDataSlice = _masterData[0..currentN];
                double avgTimeMs = task.RunMeasurement(currentDataSlice); 
                double avgTicks = avgTimeMs * TimeSpan.TicksPerMillisecond;

                task.Results.Add((currentN, avgTimeMs, avgTicks));

                // 3. Сохраняем в БД
                db.Results.Add(new ExperimentResult
                {
                    AlgorithmName = task.Name,
                    N = currentN,
                    RunNumber = 0,
                    ElapsedTimeMs = avgTimeMs,
                    ExperimentDate = experimentDate,
                    StepCount = null 
                });
            }
        
            db.SaveChanges();
        }
    }
}