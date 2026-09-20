using Algorithms.Core.Database;

namespace Algorithms.Core.MatrixAlgorithms;

public record MatrixBenchmarkResult(int N, int M, double TimeMs);

public class MatrixBenchmarker
{
    public string AlgorithmName { get; }
    private readonly Func<int, int, double> _runMeasurement; // (n, m) -> avgTimeMs

    public List<MatrixBenchmarkResult> Results { get; } = new();

    public MatrixBenchmarker(string algorithmName, Func<int, int, double> runMeasurement)
    {
        AlgorithmName = algorithmName;
        _runMeasurement = runMeasurement;
    }

    public void Run(IEnumerable<int> nValues, IEnumerable<int> mValues, bool useCache = true)
    {
        using var db = new AppDbContext();

        // Отключаем автоматическое отслеживание изменений для ускорения массовой вставки
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        var experimentDate = DateTime.Now;

        var cachedData = useCache
            ? db.Results
                .Where(r => r.AlgorithmName == AlgorithmName && r.M != null)
                .AsEnumerable() // Выгружаем отфильтрованные данные в память
                .GroupBy(r => new { r.N, M = r.M!.Value })
                .ToDictionary(
                    g => (g.Key.N, g.Key.M),
                    g => g.OrderByDescending(r => r.ExperimentDate).First().ElapsedTimeMs)
            : new Dictionary<(int N, int M), double>();

        // Временный список для накопления результатов
        var newRecords = new List<ExperimentResult>();

        foreach (int n in nValues)
        {
            foreach (int m in mValues)
            {
                // 1. Механизм кэширования
                if (useCache && cachedData.TryGetValue((n, m), out double cachedTime))
                {
                    // Добавляем в локальный результат для возврата в UI
                    Results.Add(new MatrixBenchmarkResult(n, m, cachedTime));
            
                    // Пропускаем создание новой записи в БД (как в Benchmarker_4.cs)
                    continue; 
                }

                // 2. Если в кэше нет — запускаем замер
                double timeMs = _runMeasurement(n, m);
        
                // Добавляем в локальный результат
                Results.Add(new MatrixBenchmarkResult(n, m, timeMs));

                // 3. Сохраняем в БД ТОЛЬКО новые вычисления
                newRecords.Add(new ExperimentResult
                {
                    AlgorithmName = AlgorithmName,
                    N = n,
                    M = m,
                    RunNumber = 0,
                    ElapsedTimeMs = timeMs,
                    ExperimentDate = experimentDate, 
                    StepCount = null
                });

                // Пакетное сохранение (каждые 5000 записей)
                if (newRecords.Count >= 5000)
                {
                    db.Results.AddRange(newRecords);
                    db.SaveChanges();
                    db.ChangeTracker.Clear();
                    newRecords.Clear();
                }
            }
        }

        // Сохраняем оставшиеся записи
        if (newRecords.Any())
        {
            db.Results.AddRange(newRecords);
            db.SaveChanges();
        }

        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}