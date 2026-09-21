using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
 
namespace Algorithms.GUI.Views;
 
// Отдельный partial-файл: обработчик ссылки на репозиторий,
// чтобы не трогать основной MainWindow.axaml.cs
public partial class MainWindow : Window
{
    private const string RepositoryUrl = "https://github.com/0xddi/Algorithms_Lab1";
 
    private async void GitHubLink_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri(RepositoryUrl));
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Не удалось открыть ссылку: {ex.Message}";
        }
    }
}