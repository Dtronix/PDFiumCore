git clean -fxd
dotnet build src/PDFiumCoreBindingsGenerator/PDFiumCoreBindingsGenerator.csproj -c Release
dotnet ./src/PDFiumCoreBindingsGenerator/bin/Release/net8.0/PDFiumCoreBindingsGenerator.dll latest true
dotnet pack ./src/PDFiumCore/PDFiumCore.csproj -c Release -o ./artifacts/
dotnet test ./src/PDFiumCore.Tests/PDFiumCore.Tests.csproj
pause