namespace DevStats.Plugins.CsStatistics;

public class CsFileSizeHistogramItem
{
    public string Range { get; set; } = string.Empty;
    public int Count { get; set; }
    public FileStatItem? SampleFile { get; set; } 
}