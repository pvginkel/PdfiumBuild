using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfiumBuild
{
    public class Arguments
    {
        public static Arguments Parse(string[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            string expect = null;
            string scripts = null;
            string build = null;

            foreach (string arg in args)
            {
                if (expect != null)
                {
                    switch (expect)
                    {
                        case "-s":
                            scripts = arg;
                            break;
                        case "-b":
                            build = arg;
                            break;
                    }

                    expect = null;
                    continue;
                }

                switch (arg)
                {
                    case "-s":
                    case "-b":
                        expect = arg;
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected argument " + arg);
                }
            }

            if (expect != null)
                throw new InvalidOperationException("Expected argument to " + expect);
            if (scripts == null)
                throw new InvalidOperationException("Expected -s with scripts location");
            if (build == null)
                throw new InvalidOperationException("Expected -b with build target");

            return new Arguments(scripts, build);
        }

        public string Scripts { get; }
        public string Build { get; }

        private Arguments(string scripts, string build)
        {
            Scripts = scripts;
            Build = build;
        }
    }
}
