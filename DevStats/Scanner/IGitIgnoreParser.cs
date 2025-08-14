namespace DevStats.Scanner;

public interface IGitIgnoreParser
{
  bool IsIgnored(string path, string root);
  void LoadGitIgnorePatterns(string folderPath);
}