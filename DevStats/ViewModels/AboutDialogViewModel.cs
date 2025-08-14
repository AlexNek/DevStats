using System.Reflection;

namespace DevStats.ViewModels;

public class AboutDialogViewModel
{
  public string Version => $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";
}