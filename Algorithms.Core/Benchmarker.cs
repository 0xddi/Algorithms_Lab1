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

            // Предварительно загружаем кэш для текущего алгоритма, чтобы не делать запрос в БД на каждой итерации
            var cachedData = useCache 
                ? db.Results.Where(r => r.AlgorithmName == task.Name).ToDictionary(r => r.N, r => r.ElapsedTimeMs) 
                : new Dictionary<int, double>();

            for (int i = 0; i < _masterData.Length; i++)
            {
                int currentN = i + 1;

                // 1. Механизм кэширования
                if (useCache && cachedData.TryGetValue(currentN, out double cachedTime))
                {
                    double cachedTicks = cachedTime * TimeSpan.TicksPerMillisecond;
                    task.Results.Add((currentN, cachedTime, cachedTicks));
                    continue; // Пропускаем вычисление
                }

                // 2. Если в кэше нет — запускаем замер
                var currentDataSlice = _masterData[0..currentN];
                double avgTimeMs = task.RunMeasurement(currentDataSlice); 
                double avgTicks = avgTimeMs * TimeSpan.TicksPerMillisecond;

                task.Results.Add((currentN, avgTimeMs, avgTicks));

                // 3. Сохраняем в БД (как усредненный результат)
                db.Results.Add(new ExperimentResult
                {
                    AlgorithmName = task.Name,
                    N = currentN,
                    RunNumber = 0, // 0 - агрегированный запуск
                    ElapsedTimeMs = avgTimeMs,
                    ExperimentDate = experimentDate,
                    StepCount = null 
                });
            }
            
            // Сохраняем все новые записи пакетом
            db.SaveChanges();
        }
    }
}