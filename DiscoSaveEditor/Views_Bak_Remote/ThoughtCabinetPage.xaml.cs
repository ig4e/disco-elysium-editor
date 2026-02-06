using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DiscoSaveEditor.Views;

public sealed partial class ThoughtCabinetPage : Page
{
    public ThoughtCabinetViewModel ViewModel { get; }

    public ThoughtCabinetPage()
    {
        ViewModel = App.MainViewModel.ThoughtCabinet;
        this.InitializeComponent();
    }
}
