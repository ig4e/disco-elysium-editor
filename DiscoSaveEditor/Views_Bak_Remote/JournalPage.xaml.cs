using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DiscoSaveEditor.Views;

public sealed partial class JournalPage : Page
{
    public JournalViewModel ViewModel { get; }

    public JournalPage()
    {
        ViewModel = App.MainViewModel.Journal;
        this.InitializeComponent();
    }
}
