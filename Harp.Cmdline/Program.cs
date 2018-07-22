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

                // parse

                //var testFile = new HarpFile();
                //testFile.Config.OutputDirectory = "Generated/";
                //testFile.Config.SqlConnectionString = "Server=.\\SQLEXPRESS;Database=Harp;Integrated Security=SSPI;";
                //testFile.Entities.Add(new Entity
                //{
                //    Name = "Dogs",
                //    Table = "dbo.Dogs"
                //});

                var harpFile = HarpFile.FromYaml(harpYaml);
                if (harpFile == null)
                {
                    Console.WriteLine("Invalid harp file format");
                    return;
                }

                // synchronize
                var sync = new HarpSynchronizer(new Sql(), trace);
                var sResult = sync.Synchronize(harpFile);
                Console.WriteLine($"Sync: {sResult.Code}");

                if (sResult.Code != HarpSynchronizer.SynchronizeResultCode.OK)
                    return;

                if (sResult.WasUpdated)
                {
                    var newFileContents = harpFile.GenerateYaml();
                    File.WriteAllText(harpFilePath, newFileContents);
                }

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
