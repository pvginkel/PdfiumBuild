# PdfiumBuild

This project contains a build script to build PDFium for Windows. The process to
build PDFium used here is described at
https://github.com/pvginkel/PdfiumViewer/wiki/Building-PDFium. Basically it does
a standard build, except that a `.dll` is build.

The main purpose of this project is to automate building the `pdfium.dll` support
library for the [PdfiumViewer](https://github.com/pvginkel/PdfiumViewer/) project.
However, unmodified versions of the PDFium library are also provided.

## The build server

A build server has been setup to compile PDFium daily. The results can be
downloaded from https://assendelft.webathome.org/Pdfium/. Please note that this
is a personal server, which may be down at any time. Whenever a directory
is empty, it means the build for that configuration failed. E.g. at the moment
of writing, the V8 builds are failing because of
[v8:6068](https://codereview.chromium.org/2804033005), so the configurations
that include V8 are not being built.

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
