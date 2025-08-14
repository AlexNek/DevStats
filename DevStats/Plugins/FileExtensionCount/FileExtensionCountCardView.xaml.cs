using DevStats.Helpers;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevStats.Plugins.FileExtensionCount;

/// <summary>
///     Interaction logic for WeatherUserControl.xaml
/// </summary>
public partial class FileExtensionCountCardView : UserControl
{
    public FileExtensionCountCardView()
    {
        InitializeComponent();
    }


    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var grid = sender as DataGrid;
        if (grid?.SelectedItem is ExtensionCountItem item && !string.IsNullOrEmpty(item.SampleFilePath))
        {

            FileExplorerHelper.OpenAndSelectFile(item.SampleFilePath);
            //var folder = Path.GetDirectoryName(item.SampleFilePath);
            //if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            //{
            //    Process.Start(new ProcessStartInfo
            //    {
            //        FileName = folder,
            //        UseShellExecute = true
            //    });
            //}
        }
    }
}