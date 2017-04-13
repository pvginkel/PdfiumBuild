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

        public void Execute()
        {
            CopyContrib();
            FixupBuildScript();
            GenerateBuildFiles();
            Build();
            RestoreContrib();
            CopyOutput();
        }

        private void CopyOutput()
        {
            Console.WriteLine("Copying output to target directory");

            string target = Path.Combine(_target, _script);
            Directory.CreateDirectory(target);

            string fileName = Path.Combine(_env.PdfiumPath, "out", _script, "pdfium.dll");

            if (File.Exists(fileName))
                File.Copy(fileName, Path.Combine(target, Path.GetFileName(fileName)), true);
        }

        private void FixupBuildScript()
        {
            var contribs = _contribs.ToList();

            _contribs.Add("BUILD.gn");

            string buildScript = Path.Combine(_env.PdfiumPath, "BUILD.gn");

            string target = buildScript + "-backup";

            if (File.Exists(target))
                File.Copy(target, buildScript, true);
            else
                File.Copy(buildScript, target);

            BuildScriptRewriter.Rewrite(buildScript, contribs);
        }

        private void RestoreContrib()
        {
            foreach (string fileName in _contribs)
            {
                string target = Path.Combine(_env.PdfiumPath, fileName);

                File.Delete(target);

                string source = target + "-backup";
                if (File.Exists(source))
                    File.Move(source, target);
            }
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

                string target = Path.Combine(_env.PdfiumPath, relativeName);

                Directory.CreateDirectory(Path.GetDirectoryName(target));

                if (File.Exists(target))
                {
                    string backup = target + "-backup";
                    if (!File.Exists(backup))
                        File.Move(target, backup);
                    else
                        File.Delete(target);
                }

                File.Copy(fileName, target);
            }
        }

        private void Build()
        {
            Environment.CurrentDirectory = _env.PdfiumPath;

            _env.RunCommand("ninja.exe", "-C", "out\\" + _script, "pdfium");
        }

        private void GenerateBuildFiles()
        {
            Environment.CurrentDirectory = _env.PdfiumPath;

            string args = Path.Combine(_directory, "args.gn");
            string argsTarget = Path.Combine(_env.PdfiumPath, "out", _script, "args.gn");

            if (File.Exists(args))
            {
                Console.WriteLine("Found args.gn as part of the script; copying");

                Directory.CreateDirectory(Path.GetDirectoryName(argsTarget));
                File.Copy(args, argsTarget, true);
            }
            else if (File.Exists(argsTarget))
            {
                File.Delete(argsTarget);
            }

            _env.RunCommand("gn.bat", "gen", "out\\" + _script);
        }
    }
}
