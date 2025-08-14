namespace DevStats.Models;

// Notification class for scan updates
public class ScanNotification
{
    public string FileName { get; }
    public long FileSize { get; }
    public int FileCount { get; }

    public ScanNotification(string fileName, long fileSize, int fileCount)
    {
        FileName = fileName;
        FileSize = fileSize;
        FileCount = fileCount;
    }
}