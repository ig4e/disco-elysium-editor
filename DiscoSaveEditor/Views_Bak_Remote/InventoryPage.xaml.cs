using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DiscoSaveEditor.Views;

public sealed partial class InventoryPage : Page
{
    public InventoryViewModel ViewModel { get; }

    public InventoryPage()
    {
        ViewModel = App.MainViewModel.Inventory;
        this.InitializeComponent();
    }
}
