using DevStats.Models;

namespace DevStats.Scanner;

public interface IScanProgressReporter
{
  event EventHandler<ScanNotification> OnScanUpdate;
  event EventHandler ScanCompleted;
  void NotifyScanCompleted();
  void ReportProgress(ScanNotification notification);
}