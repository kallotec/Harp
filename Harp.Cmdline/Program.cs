using Harp.Core.Services;
using System;
using System.IO;
using System.Text;

namespace Harp.Cmdline
{
    class Program
    {
        static void Main(string[] args)
        {
            var harpFilePath = Path.Combine(Environment.CurrentDirectory, "Objects.harp");
            Console.WriteLine(harpFilePath);

            StringBuilder trace;

            // read
            var reader = new HarpFileReader();
            var rResult = reader.Read(harpFilePath, out trace);
            Console.WriteLine($"Result: {rResult}");

            // make a change
            rResult.map.Entities[0].Properties[1].ColumnName = "blah";

            // write
            var writer = new HarpFileWriter();
            var wResult = writer.Write(rResult.map, harpFilePath, out trace);
            Console.WriteLine($"Result: {wResult}");

            Console.WriteLine($"----- TRACE -----");
            Console.WriteLine(trace.ToString());
            Console.WriteLine($"-----------------");

            Console.ReadLine();

        }

        static string getSqlConnectionString() => "Server=.\\SQLEXPRESS;Database=Harp;Integrated Security=SSPI;";

    }
}
