using Microsoft.UI.Xaml.Controls;
using DiscoSaveEditor.ViewModels;

namespace DiscoSaveEditor.Views;

public sealed partial class StatesPage : Page
{
    public StatesViewModel ViewModel { get; }

    public StatesPage()
    {
        this.InitializeComponent();
        ViewModel = new StatesViewModel();
        DataContext = ViewModel;
    }
}
