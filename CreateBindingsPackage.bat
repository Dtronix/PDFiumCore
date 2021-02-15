git clean -fxd
dotnet build src\PDFiumCoreBindingsGenerator\PDFiumCoreBindingsGenerator.csproj -c Release
cd .\src\PDFiumCoreBindingsGenerator\bin\Release\netcoreapp3.1
CreatePackage.bat
pause