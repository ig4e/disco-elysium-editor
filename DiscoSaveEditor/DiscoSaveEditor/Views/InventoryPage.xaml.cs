using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml;
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

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is InventoryDisplayItem item)
            ViewModel.RemoveItemCommand.Execute(item);
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is InventoryDisplayItem item)
            ViewModel.AddItemCommand.Execute(item);
    }
}
