using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DiscoSaveEditor.Views;

public sealed partial class PartyPage : Page
{
    public PartyViewModel ViewModel { get; }

    public PartyPage()
    {
        ViewModel = App.MainViewModel.Party;
        this.InitializeComponent();
    }
}
