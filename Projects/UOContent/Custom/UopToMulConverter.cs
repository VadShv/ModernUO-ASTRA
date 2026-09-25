using System;
using System.IO;
using Server;
using Server.Logging;

namespace Server.Custom;

public static class UopToMulConverter
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(UopToMulConverter));

    public static void Configure()
    {
        EventSink.ServerStarted += ConvertMaps;
    }

    private static void ConvertMaps()
    {
        var dataDir = "";
        foreach (var dir in ServerConfiguration.DataDirectories)
        {
            dataDir = dir;
            break;
        }
        var mapsToConvert = new[] { 0, 1, 2, 3, 4, 5 };

        foreach (var mapIndex in mapsToConvert)
        {
            var uopPath = Path.Combine(dataDir, $"map{mapIndex}LegacyMUL.uop");
            var mulPath = Path.Combine(dataDir, $"map{mapIndex}.mul");

            if (!File.Exists(uopPath))
            {
                continue;
            }

            if (File.Exists(mulPath))
            {
                logger.Information("UopToMul: map{Index}.mul already exists, skipping.", mapIndex);
                continue;
            }

            logger.Information("UopToMul: Converting map{Index}LegacyMUL.uop to map{Index}.mul...", mapIndex);

            try
            {
                ConvertUopToMul(uopPath, mulPath);
                logger.Information("UopToMul: Conversion complete for map{Index}.", mapIndex);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "UopToMul: Failed to convert map{Index}", mapIndex);
            }
        }
    }

    private static void ConvertUopToMul(string uopPath, string mulPath)
    {
        using var uopStream = new FileStream(uopPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var mulStream = new FileStream(mulPath, FileMode.Create, FileAccess.Write);

        var uopEntries = UOPFiles.ReadUOPIndexes(uopStream, ".dat", 0x14000, 5);

        var entries = new UOPEntry[uopEntries.Count];
        uopEntries.Values.CopyTo(entries, 0);

        Array.Sort(entries, (a, b) => a.Offset.CompareTo(b.Offset));

        using var reader = new BinaryReader(uopStream);
        for (var i = 0; i < entries.Length; ++i)
        {
            uopStream.Seek(entries[i].Offset, SeekOrigin.Begin);
            entries[i].Extra = reader.ReadInt32();
        }

        Array.Sort(entries, (a, b) => a.Extra.CompareTo(b.Extra));

        var totalSize = 0L;
        foreach (var entry in entries)
        {
            totalSize += entry.Size;
        }

        var numBlocks = totalSize / 196;
        var header = new byte[4];

        for (var block = 0L; block < numBlocks; block++)
        {
            var dataOffset = block * 196 + 4;

            var uopOffset = FindUopOffset(entries, dataOffset);
            if (uopOffset < 0)
            {
                mulStream.Write(header, 0, 4);
                var zeros = new byte[192];
                mulStream.Write(zeros, 0, 192);
                continue;
            }

            uopStream.Seek(uopOffset, SeekOrigin.Begin);

            mulStream.Write(header, 0, 4);

            var tileData = new byte[192];
            var read = 0;
            while (read < 192)
            {
                var n = uopStream.Read(tileData, read, 192 - read);
                if (n <= 0) break;
                read += n;
            }
            mulStream.Write(tileData, 0, 192);
        }
    }

    private static long FindUopOffset(UOPEntry[] entries, long offset)
    {
        var total = 0L;
        for (var i = 0; i < entries.Length; ++i)
        {
            var entry = entries[i];
            var newTotal = total + entry.Size;
            if (offset < newTotal)
            {
                return entry.Offset + (offset - total);
            }
            total = newTotal;
        }
        return -1;
    }
}
