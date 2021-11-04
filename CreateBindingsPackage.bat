git clean -fxd
dotnet build src/PDFiumCoreBindingsGenerator/PDFiumCoreBindingsGenerator.csproj -c Release
dotnet ./src/PDFiumCoreBindingsGenerator/bin/Release/net5.0/PDFiumCoreBindingsGenerator.dll latest true
dotnet pack ./src/PDFiumCore/PDFiumCore.csproj -c Release -o ./artifacts/
pause