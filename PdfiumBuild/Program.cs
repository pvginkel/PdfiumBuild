using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfiumBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Setting up environment");

            var arguments = Arguments.Parse(args);

            Console.WriteLine("Initializing build environment");

            var env = new Env(arguments);

            env.Setup();

            Console.WriteLine("Finding scripts");

            var scripts = new List<Script>();

            foreach (string directory in Directory.GetDirectories(arguments.Scripts))
            {
                Console.WriteLine($"Found script {Path.GetFileName(directory)}");

                scripts.Add(new Script(env, directory, arguments.Target));
            }

            Console.WriteLine("Executing scripts");

            bool anyFailed = false;

            foreach (var script in scripts)
            {
                if (!script.Execute())
                {
                    Console.Error.WriteLine("Compilation failed");
                    anyFailed = true;
                }
            }

            return anyFailed ? 1 : 0;
        }
    }
}
