namespace DevStats.Scanner;

public interface IFolderScanner
{
  void StartScan(string folderPath);
  void StopScan();
}