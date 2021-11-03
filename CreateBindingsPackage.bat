git clean -fxd
dotnet build src/PDFiumCoreBindingsGenerator/PDFiumCoreBindingsGenerator.csproj -c Release
cd src/PDFiumCoreBindingsGenerator/Release/net5.0/
PDFiumCoreBindingsGenerator.exe "latest" "0"
dotnet pack "../../../../PDFiumCore/PDFiumCore.csproj" -c Release -o "../../../../../artifacts/"
pause