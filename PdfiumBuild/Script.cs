using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NLog;
using PdfiumBuild.Gn;

namespace PdfiumBuild
{
	internal class Script
	{

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private readonly Env _env;
		private readonly string _directory;
		private readonly string _target;
		private readonly string _script;
		private readonly List<string> _contribs = new List<string>();

		public Architecture Architecture { get; }

		public Script(Env env, string directory, string target)
		{
			if (env == null)
				throw new ArgumentNullException(nameof(env));
			if (directory == null)
				throw new ArgumentNullException(nameof(directory));
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			_env = env;
			_directory = directory;
			_target = target;
			_script = Path.GetFileName(_directory);
			Architecture = ParseArchitecture(Path.Combine(directory, "args.gn"));
		}

		private Architecture ParseArchitecture(string path)
		{
			if (!File.Exists(path))
				throw new InvalidOperationException("Expected script directory to contain an args.gn file");

			var lexer = new GnLexer(File.ReadAllText(path));

			GnToken token;
			while ((token = lexer.Next()) != null)
			{
				if (token.Type == GnTokenType.Identifier && token.Text == "target_cpu")
				{
					token = lexer.Next();
					if (token != null && token.Type == GnTokenType.Equals)
					{
						token = lexer.Next();
						if (token != null && token.Type == GnTokenType.String)
							return ParseArchitectureValue(token.Text);
					}
				}
			}

			throw new InvalidOperationException("Missing \"target_cpu\" setting in the args.gn file");
		}

		private Architecture ParseArchitectureValue(string value)
		{
			Debug.Assert(value.StartsWith("\"") && value.EndsWith("\""));

			switch (value.Substring(1, value.Length - 2))
			{
				case "x86":
					return Architecture.X86;
				case "x64":
					return Architecture.X64;
				default:
					throw new ArgumentOutOfRangeException(nameof(value));
			}
		}

		public bool Execute()
		{

			bool result = false;

			try
			{


				logger.Info("Compiling " + _script);

				ResetBuildDirectory();
				CopyContrib();
				FixupBuildScript();
				GenerateBuildFiles();
				Build();

				string target = CopyOutput();
				if (target != null)
				{
					PublishNuGet(target);
					result = true;
				}

				result = false;
			}
			catch (Exception ex)
			{

				logger.Error(ex);
			}

			return result;
		}

		private void ResetBuildDirectory()
		{
			Environment.CurrentDirectory = _env.CheckoutPath;

			_env.RunCommand("git.exe", "clean", "-xdf", "-e", "/third_party/llvm-build");
			_env.RunCommand("git.exe", "reset", "--hard");
		}

		private string CopyOutput()
		{
			logger.Info("Copying output to target directory");

			string target = Path.Combine(_target, _script);
			Directory.CreateDirectory(target);

			string fileName = Path.Combine(_env.CheckoutPath, "out", "pdfium.dll");

			if (File.Exists(fileName))
			{
				var final = Path.Combine(target, Path.GetFileName(fileName));
				File.Copy(fileName, final, true);
				return final;
			}

			Console.Error.WriteLine($"Cannot find target at '{fileName}'");
			return null;
		}

		private void FixupBuildScript()
		{
			BuildScriptRewriter.Rewrite(Path.Combine(_env.CheckoutPath, "BUILD.gn"), _contribs);
		}

		private void CopyContrib()
		{
			string contrib = Path.Combine(_directory, "contrib");
			if (!Directory.Exists(contrib))
				return;

			foreach (string fileName in Directory.GetFiles(contrib, "*", SearchOption.AllDirectories))
			{
				if (!fileName.StartsWith(contrib))
					throw new InvalidOperationException("Expected file name to start with directory");

				string relativeName = fileName.Substring(contrib.Length).TrimStart(Path.DirectorySeparatorChar);
				_contribs.Add(relativeName);

				string target = Path.Combine(_env.CheckoutPath, relativeName);

				Directory.CreateDirectory(Path.GetDirectoryName(target));
				File.Copy(fileName, target, true);
			}
		}

		private void Build()
		{
			Environment.CurrentDirectory = _env.CheckoutPath;

			_env.RunCommand("ninja.exe", "-C", "out", "pdfium");
		}

		private void GenerateBuildFiles()
		{
			Environment.CurrentDirectory = _env.CheckoutPath;

			string args = Path.Combine(_directory, "args.gn");

			if (File.Exists(args))
			{
				logger.Info("Found args.gn as part of the script; copying");

				string target = Path.Combine(_env.CheckoutPath, "out", "args.gn");

				Directory.CreateDirectory(Path.GetDirectoryName(target));
				File.Copy(args, target, true);
			}

			_env.RunCommand("gn.bat", "gen", "out");
		}

		private void PublishNuGet(string target)
		{
			// Do we have a NuGet configuration?

			string contrib = Path.Combine(_directory, "nuget");
			if (!Directory.Exists(contrib))
				return;

			// Get the required environment variables.

			string apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
			string buildNumberString = Environment.GetEnvironmentVariable("BUILD_NUMBER");

			if (apiKey == null)
			{
				logger.Info("Skipping NuGet publish because the NUGET_API_KEY environment variable is not set");
				return;
			}
			if (buildNumberString == null || !int.TryParse(buildNumberString, out var buildNumber))
			{
				logger.Info("Skipping NuGet publish because the BUILD_NUMBER environment variable is not set or invalid");
				return;
			}

			// Find NuGet.exe.

			string nuget = Path.Combine(_env.Root, "Libraries", "NuGet", "nuget.exe");
			if (!File.Exists(nuget))
			{
				logger.Info("Skipping NuGet publish because nuget.exe was not found at '{0}'", nuget);
				return;
			}

			// Create a temporary directory to prepare the NuGet package.

			using (var path = new TempPath())
			{
				// Copy all files and transform the NuSpec file if we find it.

				string nuspecTarget = null;

				foreach (string fileName in Directory.GetFiles(contrib))
				{
					var tempTarget = Path.Combine(path.Path, Path.GetFileName(fileName));

					if (String.Equals(Path.GetExtension(fileName), ".nuspec", StringComparison.OrdinalIgnoreCase))
					{
						nuspecTarget = tempTarget;
						CopyTransformNuSpec(fileName, tempTarget, target, buildNumber);
					}
					else
					{
						File.Copy(fileName, tempTarget);
					}
				}

				if (nuspecTarget == null)
				{
					logger.Info("The nuget directory does not contain a .nuspec file; skipping NuGet publish");
					return;
				}

				// Build the NuGet package.

				_env.RunCommand(nuget, "pack", "-NoPackageAnalysis", "-NonInteractive", "-OutputDirectory", path.Path, nuspecTarget);

				// Publish the NuGet package.

				string nupkgPath = Directory.GetFiles(path.Path)
					.SingleOrDefault(p => String.Equals(Path.GetExtension(p), ".nupkg", StringComparison.OrdinalIgnoreCase));

				if (nupkgPath == null)
				{
					logger.Info("Skipping publish of NuGet package because no .nupkg file was created");
					return;
				}

				_env.RunCommand(nuget, "push", "-ApiKey", new Password(apiKey), "-Source", "https://www.nuget.org", nupkgPath);
			}
		}

		private void CopyTransformNuSpec(string source, string target, string pdfium, int buildNumber)
		{
			var now = DateTime.UtcNow;

			string versionNumber = $"{now.Year}.{now.Month}.{now.Day}.{buildNumber}";

			string nuspec = File.ReadAllText(source);
			nuspec = nuspec.Replace("$version$", versionNumber);
			nuspec = nuspec.Replace("$pdfium$", pdfium);

			File.WriteAllText(target, nuspec);
		}
	}
}
