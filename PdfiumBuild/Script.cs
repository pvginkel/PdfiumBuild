using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfiumBuild
{
    internal class Script
    {
        private readonly Env _env;
        private readonly string _directory;
        private readonly string _target;
        private readonly string _script;
        private readonly List<string> _contribs = new List<string>();

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
        }

        public bool Execute()
        {
            Console.WriteLine("Compiling " + _script);

            ResetBuildDirectory();
            CopyContrib();
            FixupBuildScript();
            GenerateBuildFiles();
            Build();

            string target = CopyOutput();
            if (target != null)
            {
                PublishNuGet(target);
                return true;
            }

            return false;
        }

        private void ResetBuildDirectory()
        {
            Environment.CurrentDirectory = _env.CheckoutPath;

            _env.RunCommand("git.exe", "clean", "-xdf", "-e", "/third_party/llvm-build");
            _env.RunCommand("git.exe", "reset", "--hard");
        }

        private string CopyOutput()
        {
            Console.WriteLine("Copying output to target directory");

            string target = Path.Combine(_target, _script);
            Directory.CreateDirectory(target);

            string fileName = Path.Combine(_env.CheckoutPath, "out", "pdfium.dll");

            if (File.Exists(fileName))
            {
                var final = Path.Combine(target, Path.GetFileName(fileName));
                File.Copy(fileName, final, true);
                return final;
            }

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
                Console.WriteLine("Found args.gn as part of the script; copying");

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
                Console.WriteLine("Skipping NuGet publish because the NUGET_API_KEY environment variable is not set");
                return;
            }
            if (buildNumberString == null || !int.TryParse(buildNumberString, out var buildNumber))
            {
                Console.WriteLine("Skipping NuGet publish because the BUILD_NUMBER environment variable is not set or invalid");
                return;
            }

            // Find NuGet.exe.

            string nuget = Path.Combine(_env.Root, "Libraries", "NuGet", "nuget.exe");
            if (!File.Exists(nuget))
            {
                Console.WriteLine("Skipping NuGet publish because nuget.exe was not found at '{0}'", nuget);
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
                    Console.WriteLine("The nuget directory does not contain a .nuspec file; skipping NuGet publish");
                    return;
                }

                // Build the NuGet package.

                _env.RunCommand(nuget, "pack", "-NoPackageAnalysis", "-NonInteractive", "-OutputDirectory", path.Path, nuspecTarget);

                // Publish the NuGet package.

                string nupkgPath = Directory.GetFiles(path.Path)
                    .SingleOrDefault(p => String.Equals(Path.GetExtension(p), ".nupkg", StringComparison.OrdinalIgnoreCase));

                if (nupkgPath == null)
                {
                    Console.WriteLine("Skipping publish of NuGet package because no .nupkg file was created");
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
