using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfiumBuild
{
	public static class Program
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public static int Main(string[] args)
		{
			bool anyFailed = false;

			try
			{

				logger.Info("Setting up environment");

				var arguments = Arguments.Parse(args);

				logger.Info("Initializing build environment");

				var env = new Env(arguments);

				env.Setup();

				logger.Info("Finding scripts");

				var scripts = new List<Script>();

				foreach (string directory in Directory.GetDirectories(arguments.Scripts))
				{
					logger.Info($"Found script {Path.GetFileName(directory)}");

					var script = new Script(env, directory, arguments.Target);
					if (script.Architecture == arguments.Architecture)
						scripts.Add(script);
					else
						logger.Info("    Skipping because of architecture mismatch");
				}

				logger.Info("Executing scripts");


				foreach (var script in scripts)
				{
					if (!script.Execute())
					{
						Console.Error.WriteLine("Compilation failed");
						anyFailed = true;
					}
				}
			}
			catch (Exception ex)
			{

				logger.Error(ex);
			}

			return anyFailed ? 1 : 0;
		}
	}
}
