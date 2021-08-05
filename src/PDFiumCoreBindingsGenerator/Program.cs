using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using CppSharp;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;

namespace PDFiumCoreBindingsGenerator
{
    class Program
    {
        private static WebClient _client;

        static void Main(string[] args)
        {
            var pdfiumReleaseGithubUrl = args[0];
            var minorBuild = args.Length > 1 ? args[1] : "0";
            var downloadBinaries = args.Length <= 2 || bool.Parse(args[2]);

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
                if (releaseInfoAsset.Name.Contains("-v8"))
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

            // Copy the binary files.
            Directory.CreateDirectory("../../../../PDFiumCore/runtimes/win-x86/native/");
            if (!EnsureCopy("pdfium-windows-x86/x86/bin/pdfium.dll", "../../../../PDFiumCore/runtimes/win-x86/native/pdfium.dll"))
                return;
            File.Copy("pdfium-windows-x86/LICENSE", "../../../../PDFiumCore/runtimes/win-x86/native/PDFium-LICENSE");

            Directory.CreateDirectory("../../../../PDFiumCore/runtimes/win-x64/native/");
            if (!EnsureCopy("pdfium-windows-x64/x64/bin/pdfium.dll", "../../../../PDFiumCore/runtimes/win-x64/native/pdfium.dll"))
                return;
            File.Copy("pdfium-windows-x64/LICENSE", "../../../../PDFiumCore/runtimes/win-x64/native/PDFium-LICENSE");

            Directory.CreateDirectory("../../../../PDFiumCore/runtimes/linux/native/");
            if (!EnsureCopy("pdfium-linux/lib/libpdfium.so", "../../../../PDFiumCore/runtimes/linux/native/pdfium.so"))
                return;
            File.Copy("pdfium-linux/LICENSE", "../../../../PDFiumCore/runtimes/linux/native/PDFium-LICENSE");

            Directory.CreateDirectory("../../../../PDFiumCore/runtimes/osx-x64/native/");
            if (!EnsureCopy("pdfium-darwin-x64/lib/libpdfium.dylib", "../../../../PDFiumCore/runtimes/osx-x64/native/pdfium.dylib"))
                return;
            File.Copy("pdfium-darwin-x64/LICENSE", "../../../../PDFiumCore/runtimes/osx-x64/native/PDFium-LICENSE");

            Directory.CreateDirectory("../../../../PDFiumCore/runtimes/osx-arm64/native/");

            if (!EnsureCopy("pdfium-darwin-arm64/lib/GNUSparseFile.0/libpdfium.dylib", "../../../../PDFiumCore/runtimes/osx-arm64/native/pdfium.dylib"))
                return;
            File.Copy("pdfium-darwin-arm64/LICENSE", "../../../../PDFiumCore/runtimes/osx-arm64/native/PDFium-LICENSE");

            var versionParts = releaseInfo.TagName.Split('/');

            // Create the version file.
            using (var stream = File.OpenWrite("../../../../Directory.Build.props"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<Project>");
                writer.WriteLine("  <PropertyGroup>");
                writer.Write("    <Version>");
                writer.Write($"{versionParts[1]}.{minorBuild}.0.0");
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
                using (var tarArchive = TarArchive.CreateInputTarArchive(inStream))
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