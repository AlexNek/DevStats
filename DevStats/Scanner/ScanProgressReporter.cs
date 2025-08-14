using DevStats.Models;

namespace DevStats.Scanner;

public class ScanProgressReporter : IScanProgressReporter
{
  public event EventHandler<ScanNotification> OnScanUpdate;
  public event EventHandler ScanCompleted;

  public void ReportProgress(ScanNotification notification)
  {
    OnScanUpdate?.Invoke(this, notification);
  }

  public void NotifyScanCompleted()
  {
    ScanCompleted?.Invoke(this, EventArgs.Empty);
  }
}