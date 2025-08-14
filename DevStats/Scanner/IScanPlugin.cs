namespace DevStats.Scanner;

public interface IScanPlugin
{
    void ProcessItem(string path, bool isFolder);
}