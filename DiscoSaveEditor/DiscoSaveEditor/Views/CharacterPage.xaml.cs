using DiscoSaveEditor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DiscoSaveEditor.Views;

public sealed partial class CharacterPage : Page
{
    public CharacterViewModel ViewModel { get; }

    public CharacterPage()
    {
        ViewModel = App.MainViewModel.Character;
        this.InitializeComponent();
    }
}
