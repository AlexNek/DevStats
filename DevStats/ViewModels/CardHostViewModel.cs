using System.Windows.Controls;
using DevStats.Dashboard;

namespace DevStats.ViewModels;

public class CardHostViewModel
{
    public ICardPlugin Plugin { get; }
    public string Title { get; }
    public object ViewModel { get; }
    public UserControl View { get; }

    public CardHostViewModel(ICardPlugin plugin)
    {
        Plugin = plugin;
        Title = plugin.Name;
        ViewModel = plugin.GetOrCreateViewModel();

        // Dynamically create the view and assign its DataContext
        if (Activator.CreateInstance(plugin.ViewType) is UserControl view)
        {
            view.DataContext = ViewModel;
            View = view;
        }
    }

    public string GetMdText()
    {
        return Plugin.GetMdText();
    }
}