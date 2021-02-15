# PDFiumCore [![NuGet](https://img.shields.io/nuget/v/PDFiumCore.svg?maxAge=60)](https://www.nuget.org/packages/PDFiumCore)

PDFiumCore contains the PDFium library binaries with a .NET Standard 2.1 wrapper containing all the exposed public header methods of the library.

Bindings are generated from the binaries and header files created at [pdfium-binaries](https://github.com/bblanchon/pdfium-binaries) repository.

#### Resources
https://pdfium.googlesource.com/pdfium/

https://github.com/bblanchon/pdfium-binaries

https://github.com/mono/CppSharp

### Build Requirements
- .NET Core 3.1

### Manual Building 

Build PDFiumCoreBindingsGenerator and edit the file at ``PDFiumCoreBindingsGenerator/bin/Release/netcoreapp3.1/CreatePackage.bat`` and change the URL to the desired release.

PDFiumCoreBindingsGenerator.exe requires the following parameters:
 - [0] Github API url for the release. (eg. https://api.github.com/repos/bblanchon/pdfium-binaries/releases/latest)  This is to determine the release version and binary assets to download.
 - [1] Version to set the Version.Minor property to.  This is used for building patches. Usually set to "0"
 - [2] Set to true to download the latest binary assets and extract.  False to use the assets if they already exist in the directory.

Execute the CreatePacakge.bat

This will do the following:
 - Download the specified files at the passed pdfium-binaries API url.
 - Extracts the zip & tgz (Actually tar) files into the current directory.
 - Opens the pdfium-windows-x64 directory and parses the header files via CppSharp and generates ``PDFiumCore.cs`` in the current directory.
 - Copies the libraries and licenses into their respecive ``src/PDFiumCore/runtimes`` directories.
 - Copies/Overrides ``src/PDFiumCore/PDFiumCore.cs`` with the newly generated ``PDFiumCore.cs``.
 - Executes ``dotnet package`` on the PDFiumCore project. and putputs the Nuget package in the project root ``output`` directory.


### ToDo
 - Create an actual parser for the comments and generate functional C# method documentation.
 - Include documentation for more than just the public methods.

### License
Matching the PDFium project, this project is released under [Apache-2.0 License](LICENSE).
