using Harp.Core.Connectors.Data;
using Harp.Core.Utilities;
using System;

namespace Harp.Cmdline
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            var result = CommandParser.Parse("get all dogs");

            Console.ReadLine();

        }
    }
}
