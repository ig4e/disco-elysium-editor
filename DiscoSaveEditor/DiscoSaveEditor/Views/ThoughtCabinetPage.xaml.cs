using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml;
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

    private void CycleState_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ThoughtDisplayItem thought)
            ViewModel.SetStateCommand.Execute(thought);
    }
}
