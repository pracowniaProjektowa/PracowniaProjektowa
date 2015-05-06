using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FileHelpers;

namespace PracowniaProjektowa
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DirectoryInfo d;
            if (args.Length < 2)
            {
                Console.WriteLine("Give path to directory with csv's next time");
                d = new DirectoryInfo(@".\..\..\data");
            }
            else
            {
                d = new DirectoryInfo(args[1]);
            }
            Console.WriteLine("Started finding files to fix ...");
            FileInfo[] files = d.GetFiles("*.csv");
            try
            {
                foreach (var file in files)
                {
                    Algorithm algorithm = new Algorithm(file);
                    algorithm.FixFile();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            sw.Stop();
            Console.WriteLine("End reading files, time : {0}", sw.Elapsed);
            Console.ReadLine();
        }
    }
}
