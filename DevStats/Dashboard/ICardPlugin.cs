namespace DevStats.Dashboard;

public interface ICardPlugin
{
    string Name { get; }
    Type ViewType { get; } // The UserControl type
    ICardViewModel GetOrCreateViewModel();
    void ProcessItem(string path, bool isFolder);

    void ClearData();
    void UpdateUi();
    string GetMdText();
}