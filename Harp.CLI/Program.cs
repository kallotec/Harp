using Harp.Core.Infrastructure;
using Harp.Core.Models;
using Harp.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Harp.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Count() < 2)
            {
                logAndTerminate("Both parameters must be specified: {command} {file}");
                return;
            }

            var cmd = args[0];
            var file = args[1];

            if (string.IsNullOrWhiteSpace(cmd) || string.IsNullOrWhiteSpace(file))
            {
                logAndTerminate("Both parameters must be specified: {command} {file}");
                return;
            }

            var actionSync = "sync";
            var actionSyncAndGenerate = "syncgen";
            var supportedActions = new[]
            {
                actionSync,
                actionSyncAndGenerate
            };

            if (!supportedActions.Any(a => cmd.ToLower() == a.ToLower()))
            {
                logAndTerminate("Command not recognized.");
                return;
            }

            if (!File.Exists(file))
            {
                file = Path.Combine(Environment.CurrentDirectory, file);

                if (!File.Exists(file))
                {
                    logAndTerminate("Could not find the specified file");
                    return;
                }
            }

            if (file.EndsWith(".harp"))
            {
                logAndTerminate("File is not a .harp file");
                return;
            }

            var verboseOutput = (args.Length > 2);

            var sb = new StringBuilder();
            var sync = new HarpSynchronizer(new Sql(), sb);

            // read file
            var harpYaml = File.ReadAllText(file);

            // load
            var loadResult = HarpFile.FromYaml(harpYaml);
            if (loadResult.code != HarpFile.ParseResult.OK)
            {
                logAndTerminate($"Could not load harp file: {loadResult.code}");
                return;
            }

            var syncSuccessful = false;

            var syncResult = sync.Synchronize(loadResult.file);

            if (syncResult.Code == HarpSynchronizer.SynchronizeResultCode.OK)
            {
                syncSuccessful = true;

                log("Sync was successful");

                if (syncResult.WasUpdated)
                    log("Harp file was updated");

                if (syncResult.UnmappedStoredProcs.Any())
                    log("There are unmapped stored procedures");

                if (syncResult.UnmappedTableColumns.Any())
                    log("There are unmapped table columns");
            }
            else
            {
                log($"Sync was NOT successful: {syncResult.Code}");

                if (syncResult.UnmappedStoredProcs.Any())
                    log("There are also some unmapped stored procedures");

                if (syncResult.UnmappedTableColumns.Any())
                    log("There are also some unmapped table columns");

                Environment.Exit(1);
            }

            if (cmd.ToLower() == actionSyncAndGenerate && syncSuccessful)
            {
                log("Not implemented");
                Environment.Exit(1);

                //var gen = new HarpGenerator();
                //gen.Generate(HarpFile,)
            }

        }

        static void log(string msg)
        {
            Console.WriteLine(msg);
        }

        static void logAndTerminate(string msg)
        {
            Console.WriteLine(msg);
            Environment.Exit(1);
        }

    }
}
