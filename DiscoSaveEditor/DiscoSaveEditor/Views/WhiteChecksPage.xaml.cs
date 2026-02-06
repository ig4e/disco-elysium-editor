using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DiscoSaveEditor.Views;

public sealed partial class WhiteChecksPage : Page
{
    public WhiteChecksViewModel ViewModel { get; }

    public WhiteChecksPage()
    {
        ViewModel = App.MainViewModel.WhiteChecks;
        this.InitializeComponent();
    }
}
