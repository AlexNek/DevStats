using System.Windows;
using DevStats.Dashboard;
using DevStats.Plugins.CsStatistics;
using DevStats.Plugins.FileExtensionCount;
using DevStats.ViewModels;
using Wpf.Ui.Appearance;

namespace DevStats;

public partial class MainWindow : Window
{
    private readonly AppSettings _appSettings = new AppSettings();
    public MainWindow()
    {
        InitializeComponent();
        ApplicationThemeManager.Apply(this);
        _appSettings.Load();

        var plugins = new List<ICardPlugin>
        {
            new FileExtensionCountCardPlugin(_appSettings),
            new CsStatisticsCardPlugin(_appSettings)
        };
        DataContext = new MainWindowViewModel(plugins,_appSettings);
    }

    
}