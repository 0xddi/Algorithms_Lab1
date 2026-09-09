namespace Algorithms.Core;

public sealed class TimeComplexity : IEquatable<TimeComplexity>
{
    // Статические экземпляры ("значения enum")
    public static readonly TimeComplexity Constant = new(1, nameof(Constant), "O(1)", "Константная сложность");
    public static readonly TimeComplexity Logarithmic = new(2, nameof(Logarithmic), "O(log n)", "Логарифмическая сложность");
    public static readonly TimeComplexity Linear = new(3, nameof(Linear), "O(n)", "Линейная сложность");
    public static readonly TimeComplexity Linearithmic = new(4, nameof(Linearithmic), "O(n log n)", "Линейно-логарифмическая сложность");
    public static readonly TimeComplexity Quadratic = new(5, nameof(Quadratic), "O(n²)", "Квадратичная сложность");
    public static readonly TimeComplexity Cubic = new(6, nameof(Cubic), "O(n³)", "Кубическая сложность");
    public static readonly TimeComplexity Exponential = new(7, nameof(Exponential), "O(2ⁿ)", "Экспоненциальная сложность");
    public static readonly TimeComplexity Factorial = new(8, nameof(Factorial), "O(n!)", "Факториальная сложность");

    public int Id { get; }
    public string Name { get; }

    private readonly string _bigONotation;
    private readonly string _description;

    private TimeComplexity(int id, string name, string bigONotation, string description)
    {
        Id = id;
        Name = name;
        _bigONotation = bigONotation;
        _description = description;
    }

    /// <summary>
    /// 1. Возвращает строковую репрезентацию в нотации O-большое (например, "O(1)", "O(n log n)").
    /// </summary>
    public string GetBigONotation() => _bigONotation;

    /// <summary>
    /// 2. Возвращает краткое описание типа сложности на русском языке.
    /// </summary>
    public string GetDescription() => _description;

    /// <summary>
    /// Возвращает перечисление всех зарегистрированных типов сложности.
    /// </summary>
    public static IReadOnlyCollection<TimeComplexity> GetAll() => new[]
    {
        Constant,
        Logarithmic,
        Linear,
        Linearithmic,
        Quadratic,
        Cubic,
        Exponential,
        Factorial
    };

    /// <summary>
    /// Поиск по числовому идентификатору.
    /// </summary>
    public static TimeComplexity? FromId(int id) 
        => GetAll().FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Поиск по имени ("Constant", "Quadratic" и т.д.).
    /// </summary>
    public static TimeComplexity? FromName(string name) 
        => GetAll().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

    public override string ToString() => $"{_bigONotation} — {_description}";

    public bool Equals(TimeComplexity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as TimeComplexity);

    public override int GetHashCode() => Id.GetHashCode();
}
