using System.ComponentModel;
using System.IO;
using System.Windows;
using DevStats.Dashboard;
using DevStats.Models;

namespace DevStats.Scanner;

public class FolderScanner : IFolderScanner, IPluginRegistry
{
  private readonly IGitIgnoreParser _gitIgnoreParser;
  private readonly IScanProgressReporter _progressReporter;
  private readonly BackgroundWorker _worker;
  private readonly List<ICardPlugin> _plugins;
  private int _fileCount;

  public FolderScanner(IGitIgnoreParser gitIgnoreParser, IScanProgressReporter progressReporter)
  {
    _gitIgnoreParser = gitIgnoreParser ?? throw new ArgumentNullException(nameof(gitIgnoreParser));
    _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
    _plugins = new List<ICardPlugin>();
    _fileCount = 0;

    _worker = new BackgroundWorker
    {
      WorkerSupportsCancellation = true,
      WorkerReportsProgress = true
    };
    _worker.DoWork += ScanFolder;
    _worker.ProgressChanged += (s, e) => _progressReporter.ReportProgress(e.UserState as ScanNotification);
  }

  public void RegisterPlugins(IReadOnlyList<ICardPlugin> plugins)
  {
    _plugins.AddRange(plugins);
  }

  public void StartScan(string folderPath)
  {
    if (!_worker.IsBusy)
    {
      _fileCount = 0;
      _gitIgnoreParser.LoadGitIgnorePatterns(folderPath);
      _worker.RunWorkerAsync(folderPath);
    }
  }

  public void StopScan()
  {
    if (_worker.IsBusy)
    {
      _worker.CancelAsync();
    }
  }

  private void ProcessDirectory(string path, BackgroundWorker worker, string root)
  {
    var folderName = Path.GetFileName(path);
    if (!string.IsNullOrEmpty(folderName) && folderName.StartsWith("."))
    {
      return;
    }

    if (worker.CancellationPending)
    {
      return;
    }

    if (_gitIgnoreParser.IsIgnored(path, root))
    {
      return;
    }

    foreach (var plugin in _plugins)
    {
      plugin.ProcessItem(path, true);
    }

    try
    {
      foreach (var file in Directory.GetFiles(path))
      {
        if (worker.CancellationPending)
        {
          return;
        }

        if (_gitIgnoreParser.IsIgnored(file, root))
        {
          continue;
        }

        var fileInfo = new FileInfo(file);
        Interlocked.Increment(ref _fileCount);
        worker.ReportProgress(0, new ScanNotification(fileInfo.Name, fileInfo.Length, _fileCount));
        foreach (var plugin in _plugins)
        {
          plugin.ProcessItem(file, false);
        }
      }

      foreach (var dir in Directory.GetDirectories(path))
      {
        if (worker.CancellationPending)
        {
          return;
        }

        if (_gitIgnoreParser.IsIgnored(dir, root))
        {
          continue;
        }

        ProcessDirectory(dir, worker, root);
      }
    }
    catch (UnauthorizedAccessException)
    {
      // Skip restricted folders/files
    }
  }

  private void ScanFolder(object sender, DoWorkEventArgs e)
  {
    var worker = sender as BackgroundWorker;
    var folderPath = e.Argument as string;

    if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
    {
      Application.Current.Dispatcher.Invoke(() =>
        MessageBox.Show("Invalid folder path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
      return;
    }

    try
    {
      ProcessDirectory(folderPath, worker!, folderPath);
    }
    catch (Exception ex)
    {
      Application.Current.Dispatcher.Invoke(() =>
        MessageBox.Show($"Error during scan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
    }
    finally
    {
      _progressReporter.NotifyScanCompleted();
    }
  }
}