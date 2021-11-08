# PDFiumCore [![NuGet](https://img.shields.io/nuget/v/PDFiumCore.svg?maxAge=60)](https://www.nuget.org/packages/PDFiumCore) ![Action Workflow](https://github.com/Dtronix/PDFiumCore/actions/workflows/dotnet.yml/badge.svg)

PDFiumCore is a .NET Standard 2.1 wrapper for the [PDFium](https://pdfium.googlesource.com/pdfium/) library which includes the [binaries](https://github.com/bblanchon/pdfium-binaries) and header pinvoke bindings.  Supports Linux-x64, OSX-x64, Win-x64, Win-x86.

Bindings are generated from the binaries and header files created at [pdfium-binaries](https://github.com/bblanchon/pdfium-binaries) repository.

### Usage

The preferred way to use this project is to use the [Nuget Package](https://www.nuget.org/packages/PDFiumCore).  This will ensure all the proper bindings in the `*.deps.json` are generated and included for the targeted environments.

### Build Requirements
- .NET 5.0

### Manual Building 

Build PDFiumCoreBindingsGenerator and edit the file at ``PDFiumCoreBindingsGenerator/bin/Release/net5.0/CreatePackage.bat`` and change the URL to the desired release.

PDFiumCoreBindingsGenerator.exe requires the following parameters:
 - [0] Set to either a specific Github API release ID for the `bblanchon/pdfium-binaries` project or `latest`. This is to determine the release version and binary assets to download.
 - [1] Set to true to download the libraries and generate the bindings.  Set to false to only download the libraries.
 - [2] Version to set the Version.Revision property to.  This is used for building patches. Usually set to "0"

Execute the CreatePacakge.bat

This will do the following:
 - Download the specified files at the passed pdfium-binaries API url.
 - Extracts the zip & tgz files into the `asset/libraries`directory.
 - Opens the pdfium-windows-x64 directory and parses the header files via CppSharp and generates ``PDFiumCore.cs`` in the current directory.
 - Copies the libraries and licenses into their respective ``src/PDFiumCore/runtimes`` directories.
 - Copies/Overrides ``src/PDFiumCore/PDFiumCore.cs`` with the newly generated ``PDFiumCore.cs``.


### ToDo
 - Create an actual parser for the comments and generate functional C# method documentation.
 - Include documentation for more than just the public methods.

### Resources

https://pdfium.googlesource.com/pdfium/

https://github.com/bblanchon/pdfium-binaries

https://github.com/mono/CppSharp

### License
Matching the PDFium project, this project is released under [Apache-2.0 License](LICENSE).
