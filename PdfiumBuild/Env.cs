﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using NLog;

namespace PdfiumBuild
{
    internal class Env
    {
		private static Logger logger = LogManager.GetCurrentClassLogger();


		private readonly string _build;
        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string _depotTools;
        private string _repo;
        private int _commandId;
        private readonly object _syncRoot = new object();

        public string Root { get; private set; }
        public string CheckoutPath { get; private set; }

        public Env(Arguments arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            _build = arguments.Build;
            Root = Path.GetDirectoryName(arguments.Scripts);

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                _environmentVariables.Add((string)entry.Key, (string)entry.Value);
            }

            logger.Info("Detecting Windows 10 SDK");

            string sdkDirectory = @"C:\Program Files (x86)\Windows Kits\10";
            string cdb = Path.Combine(sdkDirectory, "Debuggers", "x64", "cdb.exe");
            if (!File.Exists(cdb))
            {
                Console.Error.WriteLine("Cannot find the Windows 10 SDK. Please download from https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk");
                throw new InvalidOperationException("Cannot find Windows 10 SDK.");
            }

            _environmentVariables["WINDOWSSDKDIR"] = sdkDirectory;
        }

        public void Setup()
        {
            CreateBuildDirectory();
            SetupDepotTools();
            ClonePdfium();
        }

        private void ClonePdfium()
        {
            logger.Info("Getting Pdfium");

            _repo = Path.Combine(_build, "repo");

            Directory.CreateDirectory(_repo);

            Environment.CurrentDirectory = _repo;

            RunCommand("gclient.bat", "config", "--unmanaged", "https://pdfium.googlesource.com/pdfium.git");
            RunCommand("gclient.bat", "sync");

            CheckoutPath = Path.Combine(_repo, "pdfium");
        }

        private void SetupDepotTools()
        {
            _depotTools = Path.Combine(_build, "depot_tools");

            _environmentVariables["PATH"] = _depotTools + ";" + _environmentVariables["PATH"];
            _environmentVariables["DEPOT_TOOLS_WIN_TOOLCHAIN"] = "0";

            const string url = "https://storage.googleapis.com/chrome-infra/depot_tools.zip";

            logger.Info("Downloading and extracting " + url);

            string target = Path.Combine(_build, "depot_tools.zip");

            Directory.CreateDirectory(_build);

            new WebClient().DownloadFile(url, target);

            Directory.CreateDirectory(_depotTools);

            new FastZip().ExtractZip(target, _depotTools, null);

            Environment.CurrentDirectory = _depotTools;

            RunCommand("gclient.bat");
        }

        private void CreateBuildDirectory()
        {
            logger.Info($"Creating build directory at {_build}");

            if (Directory.Exists(_build))
                DirectoryEx.DeleteAll(_build, true);

            Directory.CreateDirectory(_build);

            Environment.CurrentDirectory = _build;
        }

        public void RunCommand(string exe, params object[] args)
        {
            _commandId++;

            var startInfo = new ProcessStartInfo
            {
                FileName = ResolveFromPath(exe),
                Arguments = BuildArguments(args, true),
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var entry in _environmentVariables)
            {
                startInfo.EnvironmentVariables[entry.Key] = entry.Value;
            }

            logger.Info($"Running {startInfo.FileName} {BuildArguments(args, false)}");

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = startInfo
            };

            process.OutputDataReceived += (s, e) => WriteOutput(Console.Out, e.Data, false);
            process.ErrorDataReceived += (s, e) => WriteOutput(Console.Error, e.Data, true);

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
        }

        private string BuildArguments(object[] args, bool printPasswords)
        {
            var sb = new StringBuilder();

            foreach (object arg in args)
            {
                if (sb.Length > 0)
                    sb.Append(' ');

                string printed;

                if (!printPasswords && arg is Password)
                    printed = "****";
                else
                    printed = arg.ToString();

                sb.Append("\"" + printed.Replace("\"", "\"\"") + "\"");
            }

            return sb.ToString();
        }

        private void WriteOutput(TextWriter @out, string data, bool error)
        {
            if (data == null)
                return;

            lock (_syncRoot)
            {
                var color = Console.ForegroundColor;
                if (error)
                    Console.ForegroundColor = ConsoleColor.Red;

                @out.WriteLine(_commandId + ") " + data);

                if (error)
                    Console.ForegroundColor = color;
            }
        }

        private string ResolveFromPath(string exe)
        {
            foreach (string path in _environmentVariables["PATH"].Split(';'))
            {
                string fullPath = Path.Combine(path, exe);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            throw new InvalidOperationException("Cannot find " + exe);
        }
    }
}
