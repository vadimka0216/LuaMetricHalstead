using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace LuaAnalyzConsole
{
    class Program
    {
        static void Main(string[] args)
        {
             LuaHalstead rating = new LuaHalstead();
             string path="";
             if (args.Length > 0)
                 path = args[0];
             else
             {
                 Console.WriteLine("Enter directory or path to lua script...");
                 path = Console.ReadLine();
             }

            if (Directory.Exists(path))
            {
                rating.RestartDir(new DirectoryInfo(path));
            }
            else if (File.Exists(path))
            {
                rating.RestartFile(new FileInfo(path));
            }
            rating.PrintTableOperands();
            rating.PrintTableOperators();
            rating.PrintTableAgruments();
            rating.PrintRating();

            Console.WriteLine("Please, press enter for exit this programm...");
            Console.ReadLine();
        }
    }
}
