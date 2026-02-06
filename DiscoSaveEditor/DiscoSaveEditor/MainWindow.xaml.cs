using DiscoSaveEditor.ViewModels;
using DiscoSaveEditor.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace DiscoSaveEditor;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel => App.MainViewModel;

    public MainWindow()
    {
        this.InitializeComponent();

        // Store window handle for file pickers
        var hwnd = WindowNative.GetWindowHandle(this);
        WindowHelper.ActiveWindowHandle = hwnd;

        // Set window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 850));

        // Select first nav item once a save is loaded
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsSaveLoaded) && ViewModel.IsSaveLoaded)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
            }
        };
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            var pageType = tag switch
            {
                "Character" => typeof(CharacterPage),
                "Inventory" => typeof(InventoryPage),
                "ThoughtCabinet" => typeof(ThoughtCabinetPage),
                "Journal" => typeof(JournalPage),
                "Party" => typeof(PartyPage),
                "World" => typeof(WorldPage),
                "WhiteChecks" => typeof(WhiteChecksPage),
                "Containers" => typeof(ContainersPage),
                "States" => typeof(StatesPage),
                _ => typeof(CharacterPage)
            };

            ContentFrame.Navigate(pageType);
        }
    }

    private async void SaveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is RecentSaveItem save)
        {
            await ViewModel.LoadSaveAsync(save.Path);
        }
    }
}
