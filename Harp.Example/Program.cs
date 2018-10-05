using Harp.Core.Infrastructure;
using Harp.Core.Models;
using Harp.Core.Services;
using Harp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Harp.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var trace = new StringBuilder();
            try
            {
                var harpFilePath = Path.Combine(Environment.CurrentDirectory, "DataLayer.harp");
                Console.WriteLine(harpFilePath);

                // read
                var inYaml = File.ReadAllText(harpFilePath);
                var result = HarpFile.FromYaml(inYaml);
                Console.WriteLine($"Parse: {result.code}");

                if (result.code != HarpFile.ParseResult.OK)
                    return;

                // synchronize
                var sync = new HarpSynchronizer(new Sql(), trace);
                var sResult = sync.Synchronize(result.file);
                Console.WriteLine($"Sync: {sResult.Code}");

                if (sResult.Code != HarpSynchronizer.SynchronizeResultCode.OK)
                    return;

                if (sResult.WasUpdated)
                {
                    var outYaml = result.file.GenerateYaml();
                    File.WriteAllText(harpFilePath, outYaml);
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

        HarpFile generateSampleHarpFile()
        {
            var file = new HarpFile
            {
                Config = new HarpFile.HarpConfig
                {
                    OutputDirectory = "asdf",
                    SqlConnectionString = "asdf;"
                },
                Entities = new Dictionary<string, Entity>
                    {
                        { "Dogs", new Entity
                            {
                                Name = "Thing",
                                Table = "dbo.Things",
                                Properties = new Dictionary<string, string>
                                {
                                    { "PropA", "prop_a"}
                                },
                                Behaviors = new Dictionary<string, string>
                                {
                                    { "Do thing A", "dbo.DoThingA" }
                                }
                            }
                        },
                        { "Cats", new Entity
                            {
                                Name = "Thing2",
                                Table = "dbo.Things2",
                                Properties = new Dictionary<string, string>
                                {
                                    { "PropA2", "prop_a2"}
                                },
                                Behaviors = new Dictionary<string, string>
                                {
                                    { "Do thing A2", "dbo.DoThingA2" }
                                }
                            }
                        },
                    }
            };

            return file;
        }

    }
}
