using DevStats.Dashboard;

namespace DevStats.Scanner;

public interface IPluginRegistry
{
  void RegisterPlugins(IReadOnlyList<ICardPlugin> plugins);
}