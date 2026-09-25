using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Engines.Spawners;
using Server.Items;
using Server.Logging;

namespace Server.Custom;

public static class AutoSpawn
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AutoSpawn));

    public static void Configure()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted()
    {
        var existingSpawners = World.Items.Values.OfType<ISpawner>().Count();
        if (existingSpawners > 0)
        {
            logger.Information("AutoSpawn: {Count} spawners already exist, skipping import.", existingSpawners);
            return;
        }

        logger.Information("AutoSpawn: No spawners found, importing Felucca spawns...");

        var spawnDir = Path.Combine(Core.BaseDirectory, "Data", "Spawns", "shared", "felucca");
        if (!Directory.Exists(spawnDir))
        {
            logger.Error("AutoSpawn: Spawn directory not found: {Dir}", spawnDir);
            return;
        }

        var allSpawners = new Dictionary<Guid, ISpawner>();

        var files = Directory.GetFiles(spawnDir, "*.json").OrderBy(f => f);
        foreach (var file in files)
        {
            var fi = new FileInfo(file);
            logger.Information("AutoSpawn: Importing {File}...", fi.Name);

            try
            {
                ImportSpawnersCommand.ImportFile(fi, allSpawners);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "AutoSpawn: Failed to import {File}", fi.Name);
            }
        }

        var finalCount = World.Items.Values.OfType<ISpawner>().Count();
        logger.Information("AutoSpawn: Import complete. {Count} spawners in world.", finalCount);

        if (finalCount > 0)
        {
            logger.Information("AutoSpawn: Saving world...");
            World.Save();
            logger.Information("AutoSpawn: World saved.");
        }
    }
}
