using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using DevStats.Dashboard;
using DevStats.Models;

namespace DevStats.Scanner;

public class FolderScanService
{
  public event EventHandler<ScanNotification> OnScanUpdate;
  public event EventHandler? ScanCompleted;
  private readonly AppSettings _appSettings;

  private readonly List<ICardPlugin> _plugins;
  private readonly BackgroundWorker _worker;
  private int _fileCount;
  private List<Regex>? _ignorePatterns;

  public FolderScanService(AppSettings appSettings)
  {
    _appSettings = appSettings;
    _worker = new BackgroundWorker
    {
      WorkerSupportsCancellation = true,
      WorkerReportsProgress = true
    };
    _worker.DoWork += ScanFolder;
    _worker.ProgressChanged += ReportProgress;
    _plugins = new List<ICardPlugin>();
    _fileCount = 0;
  }

  public void RegisterPlugins(List<ICardPlugin> plugins)
  {
    _plugins.AddRange(plugins);
  }

  public void StartScan(string folderPath)
  {
    if (!_worker.IsBusy)
    {
      _fileCount = 0;
      LoadGitIgnorePatterns(folderPath);
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

  private bool IsIgnored(string path, string root)
  {
    if (_ignorePatterns == null)
    {
      return false;
    }

    var relPath = Path.GetRelativePath(root, path).Replace("\\", "/");
    return _ignorePatterns.Any(r => r.IsMatch(relPath) || r.IsMatch(Path.GetFileName(path)));
  }

  private void LoadGitIgnorePatterns(string folderPath)
  {
    _ignorePatterns = null;
    if (!_appSettings.UseGitIgnore)
    {
      return;
    }

    var gitIgnorePath = Path.Combine(folderPath, ".gitignore");
    if (File.Exists(gitIgnorePath))
    {
      var lines = File.ReadAllLines(gitIgnorePath)
        .Select(l => l.Trim())
        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"));
      _ignorePatterns = lines.Select(PatternToRegex).ToList();
    }
  }

  private Regex PatternToRegex(string pattern)
  {
    // Handles simple patterns: *, ?, and directory ignores
    pattern = pattern.Replace("\\", "/");
    if (pattern.EndsWith("/"))
    {
      pattern += "*";
    }

    pattern = Regex.Escape(pattern)
      .Replace(@"\*", ".*")
      .Replace(@"\?", ".");
    return new Regex($"^{pattern}$", RegexOptions.IgnoreCase);
  }

  private void ProcessDirectory(string path, BackgroundWorker worker, string root)
  {
    // Ignore anything in a .git folder
    if (path.Contains(@"\.git\", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(@"\.git", StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    if (worker.CancellationPending)
    {
      return;
    }

    if (IsIgnored(path, root))
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

        if (IsIgnored(file, root))
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

        if (IsIgnored(dir, root))
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

  private void ReportProgress(object sender, ProgressChangedEventArgs e)
  {
    var notification = e.UserState as ScanNotification;
    OnScanUpdate?.Invoke(this, notification);
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
        MessageBox.Show($"Error during scan: {ex.Message}", "Error", MessageBoxButton.OK,
          MessageBoxImage.Error));
    }
    finally
    {
      ScanCompleted?.Invoke(this, EventArgs.Empty);
    }
  }
}