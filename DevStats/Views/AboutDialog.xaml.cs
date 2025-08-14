using DevStats.ViewModels;
using System.Windows;

namespace DevStats.Views
{
  public partial class AboutDialog : Window
  {
    public AboutDialog()
    {
      InitializeComponent();
      DataContext = new AboutDialogViewModel();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
    }
  }
}