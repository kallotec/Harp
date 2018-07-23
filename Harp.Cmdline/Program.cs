using Harp.Core.Infrastructure;
using Harp.Core.Models;
using Harp.Core.Services;
using Harp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Harp.Cmdline
{
    class Program
    {
        static void Main(string[] args)
        {
            var trace = new StringBuilder();
            try
            {
                var harpFilePath = Path.Combine(Environment.CurrentDirectory, "Objects.harp");
                Console.WriteLine(harpFilePath);

                var harpYaml = File.ReadAllText(harpFilePath);

                var parser = new HarpYamlParser();

                var parseResult = parser.Parse(harpYaml);
                Console.WriteLine($"Parse: {parseResult.code}");

                if (parseResult.code != HarpYamlParser.ParseResult.OK)
                    return;

                // synchronize
                var sync = new HarpSynchronizer(new Sql(), trace);
                var sResult = sync.Synchronize(parseResult.file);
                Console.WriteLine($"Sync: {sResult.Code}");

                if (sResult.Code != HarpSynchronizer.SynchronizeResultCode.OK)
                    return;

                //if (sResult.WasUpdated)
                //{
                //    var newFileContents = parseResult.file.GenerateYaml();
                //    File.WriteAllText(harpFilePath, newFileContents);
                //}

            }
            finally
            {
                Console.WriteLine($"----- TRACE -----");
                Console.WriteLine(trace.ToString());
                Console.WriteLine($"-----------------");

                Console.ReadLine();
            }

        }

    }
}
