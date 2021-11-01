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
            public LibInfo(string packageName, string sourceLib, string destinationLibPath)
            {
                DestinationLibPath = destinationLibPath;
                PackageName = packageName;
                SourceLib = sourceLib;
            }

        }

        static void Main(string[] args)
        {
            var gitubReleaseId = args.Length > 0 ? args[0] : "latest";
            var minorReleaseVersion = args.Length > 1 ? args[1] : "0";
            var pdfiumReleaseGithubUrl = "https://api.github.com/repos/bblanchon/pdfium-binaries/releases/"+ gitubReleaseId;
            var downloadBinaries = true;

            var libInformation = new[]
            {
                new LibInfo("pdfium-windows-x86", "x86/bin/pdfium.dll", "win-x86/native/"),
                new LibInfo("pdfium-windows-x64", "x64/bin/pdfium.dll", "win-x64/native/"),
                new LibInfo("pdfium-linux-x64", "lib/libpdfium.so", "linux-x64/native/"),
                new LibInfo("pdfium-mac-x64", "lib/libpdfium.dylib", "osx-x64/native/"),
            };

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
            Console.WriteLine("Complete.");

            foreach (var releaseInfoAsset in releaseInfo.Assets)
            {
                if (!libInformation.Any(info => releaseInfoAsset.Name.ToLower().Contains(info.PackageName)))
                    continue;

                if(downloadBinaries)
                    DownloadAndExtract(releaseInfoAsset.BrowserDownloadUrl);
            }

            // Build PDFium.cs from the windows x64 build header files.
            ConsoleDriver.Run(new PDFiumCoreLibrary("pdfium-windows-x64"));

            if (Directory.Exists("../../../../PDFiumCore/runtimes"))
                Directory.Delete("../../../../PDFiumCore/runtimes", true);

            // Add the additional build information in the header.
            var fileContents = File.ReadAllText("PDFiumCore.cs");

            using (var fs = new FileStream("../../../../PDFiumCore/PDFiumCore.cs", FileMode.Create, FileAccess.ReadWrite,
                FileShare.None))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine($"// Built from precompiled binaries at {releaseInfo.HtmlUrl}");
                sw.WriteLine($"// PDFium version {releaseInfo.TagName} [{releaseInfo.TargetCommitish}]");
                sw.WriteLine($"// Built on: {DateTimeOffset.UtcNow:R}");
                sw.Write(fileContents);
            }

            foreach (var libInfo in libInformation)
            {
                var baseOutPath = Path.Combine("../../../../PDFiumCore/runtimes/", libInfo.DestinationLibPath);
                var fileName = Path.GetFileName(libInfo.SourceLib);
                var libSourcePath = Path.Combine(libInfo.PackageName, libInfo.SourceLib);
                Directory.CreateDirectory(baseOutPath);
                if (!EnsureCopy(libSourcePath, Path.Combine(baseOutPath, fileName)))
                    return;
                File.Copy("pdfium-windows-x64/LICENSE", Path.Combine(baseOutPath, "LICENSE"));
            }

            var versionParts = releaseInfo.TagName.Split('/');

            // Create the version file.
            using (var stream = File.OpenWrite("../../../../Directory.Build.props"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<Project>");
                writer.WriteLine("  <PropertyGroup>");
                writer.Write("    <Version>");
                writer.Write($"{versionParts[1]}.{minorReleaseVersion}.0.0");
                writer.WriteLine("</Version>");
                writer.WriteLine("  </PropertyGroup>");
                writer.WriteLine("</Project>");
            }
        }

        private static void WriteError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + error);
            Console.ReadLine();
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

        private static string DownloadAndExtract(string downloadUrl)
        {
            var uri = new Uri(downloadUrl);
            var filename = Path.GetFileName(uri.LocalPath);
            var directoryName = Path.GetFileNameWithoutExtension(filename);

            if (File.Exists(filename))
                File.Delete(filename);

            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);

            Console.WriteLine($"Downloading {filename}...");

            _client.DownloadFile(downloadUrl, filename);

            Console.WriteLine("Download Complete. Unzipping...");

            if (filename.EndsWith(".zip"))
                ZipFile.ExtractToDirectory(filename, directoryName);
            else
                ExtractTGZ(filename, directoryName);

            Console.WriteLine("Unzip complete.");

            return directoryName;
        }
    }
}