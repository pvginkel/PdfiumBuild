using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfiumBuild
{
    public static class DirectoryEx
    {
        public static void Copy(string source, string target)
        {
            Copy(source, target, false);
        }

        public static void Copy(string source, string target, bool overwrite)
        {
            Directory.CreateDirectory(target);

            foreach (string directory in Directory.GetDirectories(source))
            {
                string part = Path.GetFileName(directory);

                Copy(Path.Combine(source, part), Path.Combine(target, part), overwrite);
            }

            foreach (string file in Directory.GetFiles(source))
            {
                string part = Path.GetFileName(file);

                File.Copy(Path.Combine(source, part), Path.Combine(target, part), overwrite);
            }
        }

        public static void ClearDirectory(string directory)
        {
            ClearDirectory(directory, true);
        }

        public static void ClearDirectory(string directory, bool throwOnError)
        {
            foreach (string path in Directory.GetDirectories(directory))
            {
                ClearDirectory(path, throwOnError);

                if (throwOnError)
                {
                    Directory.Delete(path);
                }
                else
                {
                    try
                    {
                        Directory.Delete(path);
                    }
                    catch
                    {
                        // Ignore exceptions.
                    }
                }
            }

            foreach (string path in Directory.GetFiles(directory))
            {
                if (throwOnError)
                {
                    Directory.Delete(path);
                }
                else
                {
                    try
                    {
                        Directory.Delete(path);
                    }
                    catch
                    {
                        // Ignore exceptions.
                    }
                }
            }
        }

        public static void DeleteAll(string path)
        {
            DeleteAll(path, false);
        }

        public static void DeleteAll(string path, bool recursive)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            foreach (string entry in Directory.GetFileSystemEntries(path))
            {
                var isDirectory = Directory.Exists(entry);

                if (isDirectory && recursive)
                    DeleteAll(entry, true);

                File.SetAttributes(entry, FileAttributes.Normal);

                if (isDirectory)
                    Directory.Delete(entry);
                else
                    File.Delete(entry);
            }
        }
    }
}
