using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Algorithms.Core.Database;
using Algorithms.GUI.Models;

namespace Algorithms.GUI.Views;

public partial class HistoryWindow : Window
{
    private readonly MainWindow? _mainWindow;

    // Пустой конструктор необходим для XAML-компилятора Avalonia
    public HistoryWindow()
    {
        InitializeComponent();
        LoadHistoryFromDb();
    }

    public HistoryWindow(MainWindow mainWindow) : this()
    {
        _mainWindow = mainWindow;
    }

    public void LoadHistoryFromDb()
    {
        using var db = new AppDbContext();
        var grouped = db.Results
            .ToList()
            .GroupBy(r => r.ExperimentDate)
            .Select(g => new HistorySession { Date = g.Key, Results = g.ToList() })
            .OrderByDescending(s => s.Date)
            .ToList();

        HistoryListBox.ItemsSource = grouped;
    }

    private List<HistorySession> GetSelectedSessions()
    {
        return HistoryListBox.SelectedItems?.Cast<HistorySession>().ToList() ?? new List<HistorySession>();
    }

    private void MenuLoadSingle_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow == null) return;
        var selected = GetSelectedSessions();
        if (!selected.Any()) return;
        
        var sessionToLoad = new List<HistorySession> { selected.First() };
        _mainWindow.CurrentLoadedSessions = sessionToLoad;
        _mainWindow.RenderComparisonCharts(sessionToLoad);
    }

    private void MenuLoadCompare_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow == null) return;
        var selected = GetSelectedSessions();
        if (selected.Count < 2) return;

        _mainWindow.CurrentLoadedSessions = selected;
        _mainWindow.RenderComparisonCharts(selected);
    }

    private void MenuCompareWithLoaded_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow == null) return;
        var selected = GetSelectedSessions();
        if (!selected.Any()) return;

        var allToCompare = new List<HistorySession>();

        if (_mainWindow.CurrentLoadedSessions.Any())
        {
            allToCompare.AddRange(_mainWindow.CurrentLoadedSessions);
            
            var existingDates = _mainWindow.CurrentLoadedSessions.Select(s => s.Date).ToHashSet();
            allToCompare.AddRange(selected.Where(s => !existingDates.Contains(s.Date)));
        }
        else
        {
            allToCompare.AddRange(selected);
        }

        _mainWindow.CurrentLoadedSessions = allToCompare;
        _mainWindow.RenderComparisonCharts(allToCompare);
    }

    private void MenuDelete_Click(object? sender, RoutedEventArgs e)
    {
        var selected = GetSelectedSessions();
        if (!selected.Any()) return;

        using var db = new AppDbContext();
        foreach (var session in selected)
        {
            var idsToRemove = session.Results.Select(r => r.Id).ToList();
            var entities = db.Results.Where(r => idsToRemove.Contains(r.Id));
            db.Results.RemoveRange(entities);
        }
        db.SaveChanges();
        LoadHistoryFromDb(); 
    }

    private void ClearDbButton_Click(object? sender, RoutedEventArgs e)
    {
        using var db = new AppDbContext();
        db.Results.RemoveRange(db.Results);
        db.SaveChanges();
        LoadHistoryFromDb();
        if (_mainWindow != null)
        {
            _mainWindow.CurrentLoadedSessions.Clear();
        }
    }
}