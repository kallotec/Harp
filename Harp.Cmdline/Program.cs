using Harp.Core.Services;
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
            try
            {
                var harpFilePath = Path.Combine(Environment.CurrentDirectory, "Objects.harp");
                Console.WriteLine(harpFilePath);

                StringBuilder trace;

                // read
                var reader = new HarpFileReader();
                var rResult = reader.Read(harpFilePath, out trace);
                Console.WriteLine($"Read: {rResult.code}");

                if (rResult.code != HarpFileReader.ReadResult.OK)
                    return;

                // synchronize
                var sync = new HarpSynchronizer(new Sql(getSqlConnectionString()));
                var sResult = sync.Synchronize(rResult.map, out trace);
                Console.WriteLine($"Sync: {sResult}");

                if (sResult != HarpSynchronizer.SynchronizeResult.OK)
                    return;

                // write
                var writer = new HarpFileWriter();
                var wResult = writer.Write(rResult.map, harpFilePath, out trace);
                Console.WriteLine($"Write: {wResult}");

                if (wResult != HarpFileWriter.WriteResult.OK)
                    return;

                Console.WriteLine($"----- TRACE -----");
                Console.WriteLine(trace.ToString());
                Console.WriteLine($"-----------------");

            }
            finally
            {
                Console.ReadLine();

            }

        }

        static string getSqlConnectionString() => "Server=.\\SQLEXPRESS;Database=Harp;Integrated Security=SSPI;";

    }
}
