using DiscoSaveEditor.Services;
using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml;

namespace DiscoSaveEditor;

public partial class App : Application
{
    private Window? _window;

    public static GameDataService GameData { get; } = new();
    public static MainViewModel MainViewModel { get; } = new(GameData);

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
