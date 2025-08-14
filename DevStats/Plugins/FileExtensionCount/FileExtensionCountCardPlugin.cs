using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using DevStats.Dashboard;

namespace DevStats.Plugins.FileExtensionCount;

public class FileExtensionCountCardPlugin : ICardPlugin
{
    private readonly AppSettings _appSettings;
    private readonly Dictionary<string, ExtensionStat> _extensionStats = new();
    private FileExtensionCountViewModel? _fileExtensionCountViewModel;
    private int _filesCount;
    private int _folderCount;

    public FileExtensionCountCardPlugin(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public string Name => "File Extensions";

    public Type ViewType => typeof(FileExtensionCountCardView);

    public void ClearData()
    {
        _folderCount = 0;
        _filesCount = 0;
        _extensionStats.Clear();
        _fileExtensionCountViewModel?.ClearData();
    }

    public string GetMdText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {Name}");
        sb.AppendLine();
        sb.AppendLine($"- **Folders scanned:** {_folderCount}");
        sb.AppendLine($"- **Files scanned:** {_filesCount}");
        sb.AppendLine($"- **Different extensions:** {_extensionStats.Count}");
        sb.AppendLine();

        if (_extensionStats.Count > 0)
        {
            sb.AppendLine("| Extension | Count |");
            sb.AppendLine("|-----------|-------|");
            foreach (var kvp in _extensionStats.OrderByDescending(e => e.Value.Count))
            {
                sb.AppendLine($"| `{kvp.Key}` | {kvp.Value.Count} |");
            }
        }
        else
        {
            sb.AppendLine("_No file extensions found._");
        }

        return sb.ToString();
    }

    

    public ICardViewModel GetOrCreateViewModel()
    {
        if (_fileExtensionCountViewModel != null)
        {
            return _fileExtensionCountViewModel;
        }

        _fileExtensionCountViewModel = new FileExtensionCountViewModel(_appSettings);
        return _fileExtensionCountViewModel;
    }

    public void ProcessItem(string path, bool isFolder)
    {
        if (isFolder)
        {
            _folderCount++;
        }
        else
        {
            _filesCount++;
            var ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
            {
                ext = ext.ToLowerInvariant();
                if (_extensionStats.TryGetValue(ext, out var stat))
                {
                    stat.Count++;
                }
                else
                {
                    _extensionStats[ext] = new ExtensionStat { Count = 1, SampleFilePath = path };
                }
            }
        }
    }

    public void UpdateUi()
    {
        if (_fileExtensionCountViewModel != null)
        {
            _fileExtensionCountViewModel.FolderCount = _folderCount;
            _fileExtensionCountViewModel.FilesCount = _filesCount;
            _fileExtensionCountViewModel.ExtensionItems = new ObservableCollection<ExtensionCountItem>(
                _extensionStats.Select(kvp => new ExtensionCountItem
                {
                    Extension = kvp.Key,
                    Count = kvp.Value.Count,
                    SampleFilePath = kvp.Value.SampleFilePath
                })
            );
            // DifferentExtensionsCount is updated automatically by the ViewModel setter
        }
    }
}