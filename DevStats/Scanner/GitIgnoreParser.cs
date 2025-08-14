using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DevStats.Scanner;

public class GitIgnoreParser : IGitIgnoreParser
{
  private readonly AppSettings _appSettings;
  private List<Regex>? _ignorePatterns;

  public GitIgnoreParser(AppSettings appSettings)
  {
    _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
  }

  public bool IsIgnored(string path, string root)
  {
    if (_ignorePatterns == null)
    {
      return false;
    }

    var relPath = Path.GetRelativePath(root, path).Replace("\\", "/");
    return _ignorePatterns.Any(r => r.IsMatch(relPath) || r.IsMatch(Path.GetFileName(path)));
  }

  public void LoadGitIgnorePatterns(string folderPath)
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
    pattern = pattern.Replace("\\", "/").Trim();
    if (string.IsNullOrEmpty(pattern))
    {
      return new Regex("$a");
    }

    var anchoredToRoot = pattern.StartsWith("/");
    if (anchoredToRoot)
    {
      pattern = pattern[1..];
    }

    var directoryOnly = pattern.EndsWith("/");
    if (directoryOnly)
    {
      pattern = pattern[..^1];
    }

    var regex = new StringBuilder();
    if (anchoredToRoot)
    {
      regex.Append("^");
    }
    else
    {
      regex.Append("^(?:.*/)?");
    }

    for (var i = 0; i < pattern.Length; i++)
    {
      var c = pattern[i];
      if (c == '*')
      {
        var isDoubleStar = i + 1 < pattern.Length && pattern[i + 1] == '*';
        if (isDoubleStar)
        {
          while (i + 1 < pattern.Length && pattern[i + 1] == '*') i++;
          regex.Append(".*");
        }
        else
        {
          regex.Append("[^/]*");
        }

        continue;
      }

      if (c == '?')
      {
        regex.Append("[^/]");
        continue;
      }

      if (c == '[')
      {
        var j = i + 1;
        var cls = new StringBuilder();
        cls.Append('[');
        var closed = false;
        for (; j < pattern.Length; j++)
        {
          cls.Append(pattern[j]);
          if (pattern[j] == ']')
          {
            closed = true;
            break;
          }
        }

        if (closed)
        {
          regex.Append(cls);
          i = j;
          continue;
        }

        regex.Append(@"\[");
        continue;
      }

      if ("+()^$.{}|".Contains(c))
      {
        regex.Append('\\').Append(c);
      }
      else
      {
        regex.Append(c);
      }
    }

    if (directoryOnly)
    {
      regex.Append("(?:/.*)?");
    }

    regex.Append("$");
    return new Regex(regex.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
  }
}