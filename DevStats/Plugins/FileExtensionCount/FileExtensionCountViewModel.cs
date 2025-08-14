using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevStats.Dashboard;

namespace DevStats.Plugins.FileExtensionCount;

public class FileExtensionCountViewModel : ObservableObject, ICardViewModel
{
    private readonly AppSettings _appSettings;
    public string Title => "File Extensions";

    private int _folderCount;
    public int FolderCount
    {
        get => _folderCount;
        set => SetProperty(ref _folderCount, value);
    }

    private int _filesCount;
    public int FilesCount
    {
        get => _filesCount;
        set => SetProperty(ref _filesCount, value);
    }

    private ObservableCollection<ExtensionCountItem> _extensionItems = new();
    public ObservableCollection<ExtensionCountItem> ExtensionItems
    {
        get => _extensionItems;
        set
        {
            if (SetProperty(ref _extensionItems, value))
            {
                DifferentExtensionsCount = _extensionItems.Count;
            }
        }
    }

    private int _differentExtensionsCount;

    public FileExtensionCountViewModel(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public int DifferentExtensionsCount
    {
        get => _differentExtensionsCount;
        set => SetProperty(ref _differentExtensionsCount, value);
    }

    public void ClearData()
    {
        FolderCount = 0;
        FilesCount = 0;
        ExtensionItems.Clear();
        DifferentExtensionsCount = 0;
    }
}