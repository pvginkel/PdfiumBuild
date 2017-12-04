# PdfiumBuild

This project contains a build script to build PDFium for Windows. The process to
build PDFium used here is described at
https://github.com/pvginkel/PdfiumViewer/wiki/Building-PDFium. Basically it does
a standard build, except that a `.dll` is build.

The main purpose of this project is to automate building the `pdfium.dll` support
library and NuGet packages for the [PdfiumViewer](https://github.com/pvginkel/PdfiumViewer/)
project. However, unmodified versions of the PDFium library are also provided.

## NuGet packages

Besides, the `pdfium.dll` files, NuGet packages are automatically build and
published by the build server.

The following NuGet packages are available:

| NuGet package                                                                                                      | Architecture | V8 support | XFA support |
| ------------------------------------------------------------------------------------------------------------------ | ------------ | ---------- | ----------- |
| [PdfiumViewer.Native.x86_64.v8-xfa](https://www.nuget.org/packages/PdfiumViewer.Native.x86_64.v8-xfa/)             | 64-bit       | Yes        | Yes         |
| [PdfiumViewer.Native.x86_64.no_v8-no_xfa](https://www.nuget.org/packages/PdfiumViewer.Native.x86_64.no_v8-no_xfa/) | 64-bit       | No         | No          |
| [PdfiumViewer.Native.x86.v8-xfa](https://www.nuget.org/packages/PdfiumViewer.Native.x86.v8-xfa/)                   | 32-bit       | Yes        | Yes         |
| [PdfiumViewer.Native.x86.no_v8-no_xfa](https://www.nuget.org/packages/PdfiumViewer.Native.x86.no_v8-no_xfa/)       | 32-bit       | No         | No          |

These NuGet packages contain the PDFium DLL and a MSBuild properties file to
copy this to the correct folder in your output directory.

Depending on your needs, you can choose the NuGet package(s) you need. The ones
with V8 and XFA support are bigger, but support more features. Also,
the V8 version does not support Windows XP so if you need support for Windows XP,
you need to choose one of the libraries that does not contain V8 support and include
an updated version of the `dbghelp` libraries. These can be found in the
[Support\dbghelp](https://github.com/pvginkel/PdfiumBuild/tree/master/Support/dbghelp) directory.

The version numbers of these packages are determined automatically and are
composed of the current date and the build number from the Jenkins build server.

## The build server

A build server has been setup to compile PDFium weekly. The results can be
downloaded from https://assendelft.webathome.org/Pdfium/. Please note that this
is a personal server, which may be down at any time.

The URL points to a website that gives a view over the build output. It shows
what builds succeeded and what builds failed and provides access to the build
results.

Builds can fail for a number of reasons. For example, at some point the builds
failed because issue [v8:6068](https://codereview.chromium.org/2804033005).
This specific issue has been solved by now, but new issues can arise.

Feel free to create an issue if the builds are failing.

## Adding build scripts

If you'd like to have a specific configuration of PDFium build, please provide
a pull request. In the project, the `Scripts` directory contains scripts to
configure a PDFium build. In such a directory, there is a `args.gn` file and
optionally a `contrib` directory. The `args.gn` file configures the GN build
tool and the `contrib` directory contains source files to be included in the
compilation. If you want to have a configuration added, please submit a pull
request with a new sub directory under the `Scripts` directory.

## Building PDFium yourself

Alternatively you can use the application to build PDFium yourself. Building
PDFium can take quite a while. To speed this up, you should remove all
configurations under the `Scripts` directory that you don't need to be build
before running the build script.
