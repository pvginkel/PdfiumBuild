using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using PdfiumBuild.Gn;

namespace PdfiumBuild
{
    public class Script
    {
        public static Script Load(string directory, Arguments arguments)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            return new Script(directory, arguments);
        }

        private readonly string _directory;
        private readonly Arguments _arguments;
        private string _build;
        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string _depotTools;
        private string _repo;
        private string _pdfium;

        private Script(string directory, Arguments arguments)
        {
            _directory = directory;
            _arguments = arguments;

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                _environmentVariables.Add((string)entry.Key, (string)entry.Value);
            }

            Console.WriteLine("Detecting Windows 10 SDK");

            string sdkDirectory = @"C:\Program Files (x86)\Windows Kits\10";
            string cdb = Path.Combine(sdkDirectory, "Debuggers", "x64", "cdb.exe");
            if (!File.Exists(cdb))
            {
                Console.Error.WriteLine("Cannot find the Windows 10 SDK. Please download from https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk");
                throw new InvalidOperationException("Cannot find Windows 10 SDK.");
            }

            _environmentVariables["WINDOWSSDKDIR"] = sdkDirectory;
        }

        public void Execute()
        {
            _build = Path.Combine(_arguments.Build, Path.GetFileName(_directory));

            // CreateBuildDirectory();
            SetupDepotTools();
            ClonePdfium();
            FixupBuildScript();
            GenerateBuildFiles();
            Build();
        }

        private void FixupBuildScript()
        {
            BuildScriptRewriter.Rewrite(Path.Combine(_pdfium, "BUILD.gn"));
        }

        private void Build()
        {
            Environment.CurrentDirectory = _pdfium;

            RunCommand("ninja.exe", "-C", "out\\release", "pdfium");
        }

        private void GenerateBuildFiles()
        {
            Environment.CurrentDirectory = _pdfium;

            string args = Path.Combine(_directory, "args.gn");
            string argsTarget = Path.Combine(_pdfium, "out", "release", "args.gn");

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

            RunCommand("gn.bat", "gen", "out\\release");
        }

        private void ClonePdfium()
        {
            Console.WriteLine("Getting Pdfium");

            _repo = Path.Combine(_build, "repo");

#if false
            Directory.CreateDirectory(_repo);

            Environment.CurrentDirectory = _repo;

            RunCommand("gclient.bat", "config", "--unmanaged", "https://pdfium.googlesource.com/pdfium.git");
            RunCommand("gclient.bat", "sync");
#endif

            _pdfium = Path.Combine(_repo, "pdfium");
        }

        private void SetupDepotTools()
        {

            _depotTools = Path.Combine(_build, "depot_tools");

            _environmentVariables["PATH"] = _depotTools + ";" + _environmentVariables["PATH"];
            _environmentVariables["DEPOT_TOOLS_WIN_TOOLCHAIN"] = "0";

#if false
            const string url = "https://storage.googleapis.com/chrome-infra/depot_tools.zip";

            Console.WriteLine("Downloading and extracting " + url);

            string target = Path.Combine(_build, "depot_tools.zip");

            new WebClient().DownloadFile(url, target);

            Directory.CreateDirectory(_depotTools);

            new FastZip().ExtractZip(target, _depotTools, null);

            Environment.CurrentDirectory = _depotTools;

            RunCommand("gclient.bat");
#endif
        }

        private void RunCommand(string exe, params string[] args)
        {
            var sb = new StringBuilder();

            foreach (string arg in args)
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                sb.Append("\"" + arg.Replace("\"", "\"\"") + "\"");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ResolveExe(exe),
                Arguments = sb.ToString(),
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var entry in _environmentVariables)
            {
                startInfo.EnvironmentVariables[entry.Key] = entry.Value;
            }

            Console.WriteLine($"Running {startInfo.FileName} {startInfo.Arguments}");

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = startInfo
            };

            process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (s, e) => Console.Error.WriteLine(e.Data);

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
        }

        private string ResolveExe(string exe)
        {
            foreach (string path in _environmentVariables["PATH"].Split(';'))
            {
                string fullPath = Path.Combine(path, exe);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            throw new InvalidOperationException("Cannot find " + exe);
        }

        private void CreateBuildDirectory()
        {
            Console.WriteLine($"Creating build directory at {_build}");

            if (Directory.Exists(_build))
                DirectoryEx.DeleteAll(_build, true);

            Directory.CreateDirectory(_build);

            Environment.CurrentDirectory = _build;
        }
    }
}
