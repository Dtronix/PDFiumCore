git clean -fxd
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" src\PDFiumCoreBindingsGenerator\PDFiumCoreBindingsGenerator.csproj -r
cd src\PDFiumCoreBindingsGenerator\bin\Debug\net48
CreatePackage.bat