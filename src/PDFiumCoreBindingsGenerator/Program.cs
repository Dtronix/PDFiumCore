using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using CppSharp;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;

namespace PDFiumCoreBindingsGenerator
{
    class Program
    {
        private static WebClient _client;

        private class LibInfo
        {
            public string PackageName { get; }
            public string SourceLib { get; }
            public string DestinationLibPath { get; }
            public string ExtractedLibBaseDirectory { get; set; }
            public LibInfo(string packageName, string sourceLib, string destinationLibPath)
            {
                DestinationLibPath = destinationLibPath;
                PackageName = packageName;
                SourceLib = sourceLib;
            }

        }

        private static string GetRootDir()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dirInfo = new DirectoryInfo(currentDir);

            while (dirInfo?.Exists == true)
            {
                var files = dirInfo.GetFiles();

                if (files.Any(f => f.Name == "README.md"))
                    return dirInfo.FullName;

                dirInfo = dirInfo.Parent;
            }

            WriteError("Could not determine project root directory.");
            throw new Exception();
        }
        static void Main(string[] args)
        {
            var gitubReleaseId = args.Length > 0 ? args[0] : "latest";
            var buildBindings = args.Length > 1 ? bool.Parse(args[1]) : true;
            var minorReleaseVersion = args.Length > 2 ? args[2] : "0";
            var pdfiumReleaseGithubUrl = "https://api.github.com/repos/bblanchon/pdfium-binaries/releases/"+ gitubReleaseId;
            var rootDir = GetRootDir();
            var solutionDir = Path.GetFullPath(Path.Combine(rootDir, "src"));
            var pdfiumProjectDir = Path.GetFullPath(Path.Combine(solutionDir, "PDFiumCore"));
            var destinationCsPath = Path.GetFullPath(Path.Combine(pdfiumProjectDir, "PDFiumCore.cs"));
            var destinationLibraryPath = Path.GetFullPath(Path.Combine(rootDir, "artifacts/libraries"));

            var win64Info = new LibInfo("pdfium-win-x64", "bin/pdfium.dll", "win-x64/native/");


            Console.WriteLine("Downloading PDFium release info...");
            _client = new WebClient();

            _client.DownloadProgressChanged += (sender, eventArgs) =>
            {
                Console.WriteLine($"{eventArgs.BytesReceived}/{eventArgs.TotalBytesToReceive}");
                Console.CursorLeft = 0;
            };

            _client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0;");
            var json = _client.DownloadString(pdfiumReleaseGithubUrl);

            Console.WriteLine("Downloaded. Reading PDFium release info...");
            var releaseInfo = JsonConvert.DeserializeObject<Release>(json);
            var versionTag = releaseInfo!.Name.Split(" ")[1];
            var versionParts = versionTag.Split(".");
            var version = new System.Version(
                int.Parse(versionParts[0]),
                int.Parse(versionParts[1]),
                int.Parse(versionParts[2]),
                int.Parse(minorReleaseVersion == "0" ? versionParts[3] : minorReleaseVersion));

            Console.WriteLine("Complete.");

            if(Directory.Exists(destinationLibraryPath))
                Directory.Delete(destinationLibraryPath, true);

            Directory.CreateDirectory(destinationLibraryPath);

            var win64Asset = releaseInfo.Assets.First(a => a.Name.ToLower().Contains(win64Info.PackageName));
            win64Info.ExtractedLibBaseDirectory = DownloadAndExtract(win64Asset.BrowserDownloadUrl, destinationLibraryPath);

            if (buildBindings)
            {
                var generatedCsPath = Path.GetFullPath(Path.Combine(win64Info.ExtractedLibBaseDirectory, "PDFiumCore.cs"));
                // Build PDFium.cs from the windows x64 build header files.
                ConsoleDriver.Run(new PDFiumCoreLibrary(win64Info.ExtractedLibBaseDirectory));

                if (Directory.Exists(Path.Combine(pdfiumProjectDir, "runtimes")))
                    Directory.Delete(Path.Combine(pdfiumProjectDir, "runtimes"), true);

                // Add the additional build information in the header.
                var fileContents = File.ReadAllText(generatedCsPath);

                using (var fs = new FileStream(destinationCsPath, FileMode.Create, FileAccess.ReadWrite,
                    FileShare.None))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"// Built from precompiled binaries at {releaseInfo.HtmlUrl}");
                    sw.WriteLine($"// Github release api {releaseInfo.Url}");
                    sw.WriteLine($"// PDFium version v{versionTag} {releaseInfo.TagName} [{releaseInfo.TargetCommitish}]");
                    sw.WriteLine($"// Built on: {DateTimeOffset.UtcNow:R}");
                    sw.Write(fileContents);
                }
            }

            if (buildBindings)
            {
                // Create the version file.
                using (var stream = File.OpenWrite(Path.Combine(solutionDir, "Directory.Build.props")))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    writer.WriteLine("<Project>");
                    writer.WriteLine("  <PropertyGroup>");
                    writer.Write("    <Version>");
                    writer.Write(version);
                    writer.WriteLine("</Version>");
                    writer.WriteLine("  </PropertyGroup>");
                    writer.WriteLine("</Project>");
                }
            }

            using (var stream = File.OpenWrite(Path.Combine(rootDir, "download_package.sh")))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("dotnet build src/PDFiumCoreBindingsGenerator/PDFiumCoreBindingsGenerator.csproj -c Release");
                writer.Write("dotnet ./src/PDFiumCoreBindingsGenerator/bin/Release/net8.0/PDFiumCoreBindingsGenerator.dll ");
                writer.Write(releaseInfo.Id);
                writer.WriteLine(" false");
            }
        }

        private static void WriteError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + error);
            throw new Exception(error);
        }

        private static bool EnsureCopy(string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                WriteError($"Could not find {sourcePath}");
                return false;
            }

            File.Copy(sourcePath, destinationPath);
            return true;
        }

        public static void ExtractTGZ(string gzArchiveName, string destFolder)
        {
            using (Stream inStream = File.OpenRead(gzArchiveName))
            {
                Stream gzipStream = new GZipInputStream(inStream);

                using (var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8))
                {
                    tarArchive.ExtractContents(destFolder);
                }
            }
        }

        private static string DownloadAndExtract(string downloadUrl, string baseDestination)
        {
            var uri = new Uri(downloadUrl);
            var filename = Path.GetFileName(uri.LocalPath);
            var fullFilePath = Path.Combine(baseDestination, filename);
            var destinationDirPath = Path.Combine(baseDestination, Path.GetFileNameWithoutExtension(filename));

            if (File.Exists(fullFilePath))
                File.Delete(fullFilePath);

            if (Directory.Exists(destinationDirPath))
                Directory.Delete(destinationDirPath, true);

            Console.WriteLine($"Downloading {filename}...");

            _client.DownloadFile(downloadUrl, fullFilePath);

            Console.WriteLine("Download Complete. Unzipping...");

            if (filename.EndsWith(".zip"))
                ZipFile.ExtractToDirectory(fullFilePath, destinationDirPath);
            else
                ExtractTGZ(fullFilePath, destinationDirPath);

            Console.WriteLine("Unzip complete.");

            return destinationDirPath;
        }
    }
}