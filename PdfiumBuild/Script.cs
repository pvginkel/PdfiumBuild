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
            return CopyOutput();
        }

        private void ResetBuildDirectory()
        {
            Environment.CurrentDirectory = _env.CheckoutPath;

            _env.RunCommand("git.exe", "clean", "-xdf", "-e", "/third_party/llvm-build");
            _env.RunCommand("git.exe", "reset", "--hard");
        }

        private bool CopyOutput()
        {
            Console.WriteLine("Copying output to target directory");

            string target = Path.Combine(_target, _script);
            Directory.CreateDirectory(target);

            string fileName = Path.Combine(_env.CheckoutPath, "out", "pdfium.dll");

            if (File.Exists(fileName))
            {
                File.Copy(fileName, Path.Combine(target, Path.GetFileName(fileName)), true);
                return true;
            }

            return false;
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
    }
}
