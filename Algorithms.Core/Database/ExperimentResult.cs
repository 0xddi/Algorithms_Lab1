using System.ComponentModel.DataAnnotations;

namespace Algorithms.Core.Database;

public class ExperimentResult
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string AlgorithmName { get; set; } = string.Empty;
    
    public int N { get; set; }
    
    /// <summary>
    /// Используется только для двухпараметрических экспериментов (умножение матриц); null для остальных
    /// </summary>
    public int? M { get; set; }
    
    /// <summary>
    /// Номер запуска (1..5). Если 0 — это усредненный/кэшированный результат.
    /// </summary>
    public int RunNumber { get; set; } 
    
    public double ElapsedTimeMs { get; set; }
    
    public long? StepCount { get; set; } // Для Части IV (возведение в степень)
    
    public DateTime ExperimentDate { get; set; }
}