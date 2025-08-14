using System.Windows;
using DevStats.ViewModels;

namespace DevStats.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog(AppSettings appSettings)
    {
        InitializeComponent();
        DataContext = new SettingsDialogViewModel(appSettings);
    }
}