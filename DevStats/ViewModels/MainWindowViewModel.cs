using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevStats.Dashboard;
using DevStats.Models;
using DevStats.Properties;
using DevStats.Scanner;
using DevStats.Views;
using Microsoft.Win32;

namespace DevStats.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
  private readonly AppSettings _appSettings;

  //private readonly FolderScanService _scanService;
  private readonly FolderScanner _folderScanner;
  private readonly List<ICardPlugin> _plugins;
  [ObservableProperty] private bool _isScanFlyoutOpen;
  [ObservableProperty] private bool _isScanRunning;
  [ObservableProperty] private string _openedFolderPath;
  [ObservableProperty] private string _scanStatus;

  public ObservableCollection<CardHostViewModel> Cards { get; } = new();

  public RelayCommand CommandCancelScan { get; }

  public ICommand CommandCopyCard { get; }
  public RelayCommand CommandExit { get; }
  public RelayCommand CommandOpenFolder { get; }
  public ICommand CommandSettings { get; }
  public ICommand CommandShowAbout { get; }
  public RelayCommand CommandStartScan { get; }

  public string WindowTitle
  {
    get
    {
      var version = Assembly.GetExecutingAssembly().GetName().Version;
      return $"Developer statistic v{version?.ToString(3) ?? "1.0.0"}";
    }
  }

  public MainWindowViewModel(List<ICardPlugin> plugins, AppSettings appSettings)
  {
    _plugins = plugins;
    _appSettings = appSettings;
    CommandExit = new RelayCommand(OnExit);
    CommandOpenFolder = new RelayCommand(OnOpenFolder);
    CommandStartScan = new RelayCommand(OnStartScan, CanStartScan);
    CommandCancelScan = new RelayCommand(OnCancelScan, CanCancelScan);
    CommandCopyCard = new RelayCommand<CardHostViewModel>(CopyCardToClipboard);
    CommandSettings = new RelayCommand(OpenSettingsDialog);
    CommandShowAbout = new RelayCommand(ShowAbout);


    IsScanFlyoutOpen = false;
    OpenedFolderPath = Settings.Default.LastOpenedFolderPath;
    ScanStatus = "Ready";

    foreach (var plugin in plugins)
    {
      Cards.Add(new CardHostViewModel(plugin));
    }

    // Initialize refactored classes
    var gitIgnoreParser = new GitIgnoreParser(appSettings);
    var progressReporter = new ScanProgressReporter();
    _folderScanner = new FolderScanner(gitIgnoreParser, progressReporter);

    // Subscribe to events on ScanProgressReporter
    progressReporter.OnScanUpdate += OnScanUpdate;
    progressReporter.ScanCompleted += OnScanServiceOnScanCompleted;

    // Register plugins with FolderScanner
    _folderScanner.RegisterPlugins(plugins);
  }

  public void UpdateAllCardPluginsUi()
  {
    //foreach (var card in Cards)
    //{
    //    if (card.ViewModel is ICardPlugin plugin)
    //    {
    //        plugin.UpdateUi();
    //    }
    //}

    foreach (var plugin in _plugins)
    {
      plugin.UpdateUi();
    }
  }

  private bool CanCancelScan()
  {
    return IsScanRunning;
  }

  private bool CanStartScan()
  {
    return !IsScanRunning;
  }

  private void CopyCardToClipboard(CardHostViewModel? card)
  {
    if (card == null)
    {
      return;
    }

    // This will later call plugin.GetMdText(card) instead
    var mdText = card.GetMdText();
    Clipboard.SetText(mdText);
  }

  private void OnCancelScan()
  {
    _folderScanner.StopScan();
    IsScanRunning = false;
    IsScanFlyoutOpen = false;
    ScanStatus = "Scan cancelled.";
    CommandStartScan.NotifyCanExecuteChanged();
    CommandCancelScan.NotifyCanExecuteChanged();
    UpdateAllCardPluginsUi();
  }

  private void OnExit()
  {
    Application.Current.Shutdown();
  }

  partial void OnIsScanRunningChanged(bool value)
  {
    CommandStartScan.NotifyCanExecuteChanged();
    CommandCancelScan.NotifyCanExecuteChanged();
  }

  private void OnOpenFolder()
  {
    var dialog = new OpenFolderDialog();
    if (dialog.ShowDialog() == true)
    {
      OpenedFolderPath = dialog.FolderName;
      Settings.Default.LastOpenedFolderPath = OpenedFolderPath;
      Settings.Default.Save();
      ScanStatus = "Folder selected. Ready to scan.";
      OnStartScan();
    }
  }

  private void OnScanServiceOnScanCompleted(object? sender, EventArgs e)
  {
    Application.Current.Dispatcher.Invoke(() =>
    {
      IsScanRunning = false;
      IsScanFlyoutOpen = false;
      ScanStatus = "Scan completed.";
      CommandStartScan.NotifyCanExecuteChanged();
      CommandCancelScan.NotifyCanExecuteChanged();
      UpdateAllCardPluginsUi();
    });
  }

  private void OnScanUpdate(object sender, ScanNotification notification)
  {
    Application.Current.Dispatcher.Invoke(() => { ScanStatus = $"Scanning... Files found: {notification.FileCount}"; });
  }

  private void OnStartScan()
  {
    if (string.IsNullOrEmpty(OpenedFolderPath))
    {
      ScanStatus = "Please select a folder to scan.";
      return;
    }

    foreach (var plugin in _plugins)
    {
      plugin.ClearData();
    }

    IsScanRunning = true;
    IsScanFlyoutOpen = true;
    ScanStatus = "Scanning started...";
    CommandStartScan.NotifyCanExecuteChanged();
    CommandCancelScan.NotifyCanExecuteChanged();
    _folderScanner.StartScan(OpenedFolderPath);
  }

  private void OpenSettingsDialog()
  {
    var dlg = new SettingsDialog(_appSettings);
    dlg.Owner = Application.Current.MainWindow;
    dlg.ShowDialog();
  }

  private void ShowAbout()
  {
    var dlg = new AboutDialog();
    dlg.Owner = Application.Current.MainWindow;
    dlg.ShowDialog();
  }
}