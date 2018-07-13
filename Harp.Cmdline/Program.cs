using Harp.Core.Connectors.Data;
using Harp.Core.Generators;
using Harp.Core.Utilities;
using System;
using System.IO;
using System.Text;

namespace Harp.Cmdline
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //var result = CommandParser.Parse("get all dogs");

            var harpFilePath = Path.Combine(Environment.CurrentDirectory, "Objects.harp");
            Console.WriteLine(harpFilePath);

            var outputFolder = Path.Combine(Environment.CurrentDirectory, "Generated");

            var gen = new Generator();
            StringBuilder trace;
            gen.Generate(harpFilePath, getSqlConnectionString(), outputFolder, out trace);

            Console.ReadLine();

        }

        static string getSqlConnectionString() => "Server=.\\SQLEXPRESS;Database=Harp;Integrated Security=SSPI;";

    }
}
