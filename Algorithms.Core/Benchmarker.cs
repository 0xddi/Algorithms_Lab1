using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.Core.Database;

namespace Algorithms.Core;


public class BenchmarkProgressState
{
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string CurrentTaskName { get; set; } = string.Empty;
}

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
    // Измененная сигнатура с поддержкой отмены и прогресса
    public void RunFiltered(IEnumerable<BenchmarkTask> tasksToRun, bool useCache = true, int benchCycles = 5,
                            CancellationToken token = default, 
                            IProgress<BenchmarkProgressState>? progress = null, 
                            BenchmarkProgressState? progressState = null)
    {
        using var db = new AppDbContext();
        var experimentDate = DateTime.Now;

        foreach (var task in tasksToRun)
        {
            task.Results.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var cachedData = useCache 
                ? db.Results
                    .Where(r => r.AlgorithmName == task.Name)
                    .GroupBy(r => r.N)
                    .ToDictionary(g => g.Key, g => 
                    {
                        var r = g.OrderByDescending(x => x.ExperimentDate).First();
                        return task.IsStepMeasurement ? (double)(r.StepCount ?? 0) : r.ElapsedTimeMs;
                    })
                : new Dictionary<int, double>();

            try
            {
                for (int i = 0; i < _masterData.Length; i++)
                {
                    // Проверка на то, была ли запрошена отмена
                    token.ThrowIfCancellationRequested();

                    int currentN = i + 1;

                    if (useCache && cachedData.TryGetValue(currentN, out double cachedTime))
                    {
                        double cachedTicks = cachedTime * TimeSpan.TicksPerMillisecond;
                        task.Results.Add((currentN, cachedTime, cachedTicks));
                    }
                    else
                    {
                        var currentDataSlice = _masterData[0..currentN];
                        double avgTimeMs = task.RunMeasurement(currentDataSlice); 
                        double avgTicks = avgTimeMs * TimeSpan.TicksPerMillisecond;

                        task.Results.Add((currentN, avgTimeMs, avgTicks));

                        db.Results.Add(new ExperimentResult
                        {
                            AlgorithmName = task.Name,
                            N = currentN,
                            RunNumber = 0,
                            ElapsedTimeMs = task.IsStepMeasurement ? 0 : avgTimeMs,
                            ExperimentDate = experimentDate,
                            StepCount = null 
                        });
                    }

                    // Обновляем UI
                    if (progressState != null && progress != null)
                    {
                        progressState.CurrentStep++;
                        progressState.CurrentTaskName = task.Name;
                        progress.Report(progressState);
                    }
                }
                
                db.SaveChanges(); // Сохраняем завершенный алгоритм
            }
            catch (OperationCanceledException)
            {
                // Аккуратная работа с БД: если нажали отмену, сохраняем всё, что успели посчитать!
                db.SaveChanges(); 
                throw; // Пробрасываем ошибку дальше для остановки
            }
        }
    }
}