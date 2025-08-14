using System.Diagnostics;

namespace DevStats.Plugins.CsStatistics;

public static class CsFileSizeHistogramBuilder
{
    public static List<CsFileSizeHistogramItem> BuildWithSamples(List<FileStatItem> files, int bucketCount)
    {
        if (files == null || files.Count == 0)
        {
            return new List<CsFileSizeHistogramItem>();
        }

        // Use your existing bucket logic, but work with FileStatItem
        var sizes = files.Select(f => f.Size).ToList();
        var histogram = Build(sizes, bucketCount);

        // For each bucket, set SampleFile to the largest file in that range
        foreach (var item in histogram)
        {
            // Parse the range start/end from your bucket string
            var parts = item.Range.Split('-');
            var start = ParseSize(parts[0]);
            var end = ParseSize(parts[1]);

            var filesInBucket = files.Where(f => f.Size >= start && f.Size <= end).ToList();
            item.SampleFile = filesInBucket.OrderByDescending(f => f.Size).FirstOrDefault();
        }

        return histogram;
    }

    private static List<CsFileSizeHistogramItem> Build(List<long> sizes, int maxRanges)
    {
        if (sizes.Count == 0)
        {
            return new List<CsFileSizeHistogramItem>();
        }

        // Debug: Log input stats using Trace
        var sortedSizes = sizes.OrderBy(s => s).ToList();
        Trace.WriteLine($"Input: {sortedSizes.Count} files, Min: {sortedSizes[0]} bytes, Max: {sortedSizes[^1]} bytes");

        var binCount = Math.Min(maxRanges, sortedSizes.Count);

        // Calculate boundaries
        var boundaries = CalculateBoundaries(sortedSizes, binCount);

        var items = new List<CsFileSizeHistogramItem>();
        var binnedSizes = new HashSet<long>(); // Track binned sizes
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var start = boundaries[i];
            var end = boundaries[i + 1];

            var count = sortedSizes.Count(sv => sv >= start && sv <= end);
            if (count > 0)
            {
                items.Add(new CsFileSizeHistogramItem
                {
                    Range = $"{FormatSize(start, end)} - {FormatSize(end, start)}",
                    Count = count
                });
                foreach (var size in sortedSizes.Where(sv => sv >= start && sv <= end))
                {
                    binnedSizes.Add(size);
                }
            }
        }

        // Debug: Check for unbinned files
        var unbinnedSizes = sortedSizes.Except(binnedSizes).ToList();
        if (unbinnedSizes.Any())
        {
            Trace.WriteLine(
                $"Error: {unbinnedSizes.Count} files not binned: Range {unbinnedSizes.Min()} to {unbinnedSizes.Max()} bytes");
            Trace.WriteLine(
                $"Unbinned sizes (first 10): {string.Join(", ", unbinnedSizes.Take(10))}{(unbinnedSizes.Count > 10 ? "..." : "")}");
        }

        // Debug: Verify total count
        var totalCount = items.Sum(item => item.Count);
        if (totalCount != sortedSizes.Count)
        {
            Trace.WriteLine($"Error: Histogram count {totalCount} does not match input count {sortedSizes.Count}");
        }

        // Merge small bins (count < 50)
        return MergeSmallBins(items, 50);
    }

    private static List<long> CalculateBoundaries(List<long> sortedSizes, int binCount)
    {
        var boundaries = new List<long> { sortedSizes[0] }; // Start at min size (3 bytes)
        var maxSize = sortedSizes[^1];

        // Use boundaries matching the provided histogram
        var predefinedBoundaries = new long[]
        {
            100, 200, 300, 1024, // Bytes
            10 * 1024, 20 * 1024, 30 * 1024, 40 * 1024, 50 * 1024, // 1-50 KB
            100 * 1024, 150 * 1024, 200 * 1024, 300 * 1024, 600 * 1024, 650 * 1024 // 50 KB+
        };

        foreach (var boundary in predefinedBoundaries)
        {
            if (boundary > boundaries[^1] && boundary < maxSize && boundaries.Count < binCount - 1)
            {
                boundaries.Add(boundary);
            }
        }

        // Ensure the last boundary is the exact max size
        if (maxSize > boundaries[^1])
        {
            boundaries.Add(maxSize);
        }

        return boundaries;
    }

    private static long DetermineBinWidth(long range, int binCount)
    {
        var idealWidth = range / binCount;

        if (idealWidth < 512)
        {
            return 100; // 100 bytes for small sizes
        }

        if (idealWidth < 5 * 1024)
        {
            return 512; // 0.5 KB for small-mid sizes
        }

        if (idealWidth < 500 * 1024)
        {
            return 10 * 1024; // 10 KB for mid sizes
        }

        return 50 * 1024; // 50 KB for large sizes
    }

    private static string FormatSize(long bytes, long? adjacentBoundary = null)
    {
        // Use KB for sizes >= 1 KB, round up for precision
        if (bytes >= 1024)
        {
            return $"{(int)Math.Ceiling(bytes / 1024.0)} KB";
        }

        return $"{bytes} B";
    }

    private static long GetNiceBoundary(long value)
    {
        if (value >= 1024 * 1024) // Preserve exact max size (e.g., 1,357,522 bytes)
        {
            return value;
        }

        if (value < 512)
        {
            return value / 100 * 100 == 0 ? 100 : value / 100 * 100; // Round to 100 bytes
        }

        if (value < 5 * 1024)
        {
            return (long)(Math.Ceiling(value / 512.0) * 512); // Round up to nearest 0.5 KB
        }

        if (value < 500 * 1024)
        {
            return (long)(Math.Ceiling(value / (10.0 * 1024)) * (10 * 1024)); // Round up to nearest 10 KB
        }

        return (long)(Math.Ceiling(value / (50.0 * 1024)) * (50 * 1024)); // Round up to nearest 50 KB
    }

    private static List<CsFileSizeHistogramItem> MergeSmallBins(List<CsFileSizeHistogramItem> items, int minCount)
    {
        if (items.Count <= 1)
        {
            return items;
        }

        var merged = new List<CsFileSizeHistogramItem>();
        var current = items[0];

        for (var i = 1; i < items.Count; i++)
            if (current.Count < minCount && i < items.Count - 1) // Don't merge the last bin
            {
                var next = items[i];
                var currentEnd = ParseRangeEnd(current.Range);
                var nextEnd = ParseRangeEnd(next.Range);
                var currentStart = ParseRangeStart(current.Range);
                current.Range = $"{FormatSize(currentStart, nextEnd)} - {FormatSize(nextEnd, currentStart)}";
                current.Count += next.Count;
            }
            else
            {
                merged.Add(current);
                current = items[i];
            }

        merged.Add(current); // Add the last bin

        return merged;
    }

    private static long ParseRangeEnd(string range)
    {
        var endStr = range.Split('-')[1].Trim().Split(' ')[0];
        var unit = range.Split('-')[1].Trim().Split(' ')[1];
        var endValue = long.Parse(endStr);
        if (unit == "KB")
        {
            return endValue * 1024;
        }

        if (unit == "MB")
        {
            return endValue * 1024 * 1024;
        }

        return endValue;
    }

    private static long ParseRangeStart(string range)
    {
        var startStr = range.Split('-')[0].Trim().Split(' ')[0];
        var unit = range.Split('-')[0].Trim().Split(' ')[1];
        var startValue = long.Parse(startStr);
        if (unit == "KB")
        {
            return startValue * 1024;
        }

        if (unit == "MB")
        {
            return startValue * 1024 * 1024;
        }

        return startValue;
    }

    // Helper to parse size from string (e.g., "3 KB" or "1357522 bytes")
    private static long ParseSize(string sizeStr)
    {
        sizeStr = sizeStr.Trim();
        if (sizeStr.EndsWith("KB"))
        {
            return long.Parse(sizeStr.Replace("KB", "").Trim()) * 1024;
        }

        if (sizeStr.EndsWith("MB"))
        {
            return long.Parse(sizeStr.Replace("MB", "").Trim()) * 1024 * 1024;
        }

        if (sizeStr.EndsWith("B"))
        {
            return long.Parse(sizeStr.Replace("B", "").Trim());
        }

        return long.Parse(sizeStr);
    }
}