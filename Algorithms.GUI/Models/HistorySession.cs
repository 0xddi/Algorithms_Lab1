
using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.Core.Database;

namespace Algorithms.GUI.Models;
public class HistorySession
{
    public DateTime Date { get; set; }
    public string DisplayName => $"Сессия от {Date:g}";
    public List<ExperimentResult> Results { get; set; } = new();
    
    public string AlgorithmsSummary => string.Join(", ", Results.Select(r => r.AlgorithmName).Distinct());
}