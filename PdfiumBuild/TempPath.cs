using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfiumBuild
{
    internal class TempPath : IDisposable
    {
        private bool _disposed;

        public string Path { get; }

        public TempPath()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    DirectoryEx.DeleteAll(Path, true);
                }
                catch
                {
                    // Ignore exceptions.
                }

                _disposed = true;
            }
        }
    }
}
