using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DiscoSaveEditor.ViewModels;

namespace DiscoSaveEditor.Views;

public sealed partial class WorldPage : Page
{
    public WorldViewModel ViewModel { get; }

    public WorldPage()
    {
        this.InitializeComponent();
        ViewModel = App.MainViewModel.World;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (App.MainViewModel.CurrentSave != null)
        {
            ViewModel.LoadFromSave(App.MainViewModel.CurrentSave);
        }
    }
}
