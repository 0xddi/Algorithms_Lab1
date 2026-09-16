using Algorithms.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Algorithms.GUI;

// Наследуем от ObservableObject для реактивности UI
public partial class AlgorithmTaskItem : ObservableObject
{
    [ObservableProperty] 
    private string _name = string.Empty;
    
    [ObservableProperty] 
    private bool? _isSelected = true; 
    
    [ObservableProperty] 
    private BenchmarkTask _task = null!;
}