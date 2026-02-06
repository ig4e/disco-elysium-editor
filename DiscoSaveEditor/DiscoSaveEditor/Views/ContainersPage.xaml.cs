using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using DiscoSaveEditor.ViewModels;

namespace DiscoSaveEditor.Views;

public sealed partial class ContainersPage : Page
{
    public ContainersViewModel ViewModel { get; }

    public ContainersPage()
    {
        this.InitializeComponent();
        ViewModel = new ContainersViewModel();
        DataContext = ViewModel;
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ContainerItemDisplay item)
        {
            ViewModel.RemoveItemCommand.Execute(item);
        }
    }
}
