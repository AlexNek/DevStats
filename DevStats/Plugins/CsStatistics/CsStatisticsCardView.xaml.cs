using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevStats.Helpers;
using ScottPlot;
using ScottPlot.Plottables; // Required for Bar class

namespace DevStats.Plugins.CsStatistics
{
    public partial class CsStatisticsCardView : UserControl
    {
        public CsStatisticsCardView()
        {
            InitializeComponent();
        }

        private void HistogramGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem is CsFileSizeHistogramItem item && item.SampleFile != null)
            {
                FileExplorerHelper.OpenAndSelectFile(item.SampleFile.FilePath);
            }
        }

        private void HistogramPlot_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CsStatisticsViewModel vm && vm.HistogramCounts.Length > 0)
            {
                HistogramPlot.Plot.Clear();

                // Add individual bars
                for (int i = 0; i < vm.HistogramCounts.Length; i++)
                {
                    var bar = new Bar
                    {
                        Value = vm.HistogramCounts[i], // Bar length
                        Position = i, // Y-axis position for horizontal bars
                        //IsHorizontal = true, // Horizontal orientation
                        Label = vm.HistogramLabels != null && i < vm.HistogramLabels.Length ? vm.HistogramLabels[i] : null // Category label
                    };
                    HistogramPlot.Plot.Add.Bar(bar);
                }

                // Set plot title
                HistogramPlot.Plot.Title(vm.Title);

                // Set axis labels
                HistogramPlot.Plot.Axes.Left.Label.Text = "Categories"; // Y-axis (categories)
                HistogramPlot.Plot.Axes.Bottom.Label.Text = "Files Count"; // X-axis (values)

                // Set minimum for the X-axis (bottom axis for horizontal bars)
                //HistogramPlot.Plot.Axes.Bottom.Minimum = 0;

                // Configure Y-axis ticks for category labels
                if (vm.HistogramLabels != null)
                {
                    var tickPositions = Enumerable.Range(0, vm.HistogramLabels.Length).Select(i => (double)i).ToArray();
                    HistogramPlot.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickPositions, vm.HistogramLabels);
                }

                // Ensure no padding beneath the bars
                HistogramPlot.Plot.Axes.Margins(bottom: 0);

                // Refresh the plot
                HistogramPlot.Refresh();
            }
        }
    }
}