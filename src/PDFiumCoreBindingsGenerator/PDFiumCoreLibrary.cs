using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace PDFiumCoreBindingsGenerator
{
    class PDFiumCoreLibrary : ILibrary
    {
        private readonly string _directoryName;
        private readonly string _exeLocation;

        public PDFiumCoreLibrary(string directoryName)
        {
            _directoryName = directoryName;
            _exeLocation = Path.GetDirectoryName(typeof(PDFiumCoreLibrary).Assembly.Location);

        }


        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
            // Fix for generating code which will not compile.
            var fpdfLibraryConfig = ctx.FindClass("FPDF_LIBRARY_CONFIG_");
            fpdfLibraryConfig.First().Properties.First(f => f.OriginalName == "m_pUserFontPaths").Ignore = true;
        }


        public void Setup(Driver driver)
        {
            var includeDirectory = Path.Combine(_directoryName, "include");
            driver.ParserOptions.SetupMSVC(VisualStudioVersion.Latest);
            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            //options.Verbose = true;
            options.CommentKind = CommentKind.BCPLSlash;
            options.OutputDir = _directoryName;

            driver.Options.ZeroAllocatedMemory = cls => {
                return cls.QualifiedName == "FPDF_FORMFILLINFO";
            };

            var module = options.AddModule("PDFiumCore");
            module.SharedLibraryName = "pdfium";

            // Ensure that the win32 includes are ignored.
            module.Undefines.Add("_WIN32");

            module.IncludeDirs.Add(Path.Combine(_exeLocation, "lib/clang/14.0.0/include"));
            module.IncludeDirs.Add(includeDirectory);
            module.IncludeDirs.Add(Path.Combine(includeDirectory, "cpp"));

            var dirinfo = new DirectoryInfo(includeDirectory);
            
            foreach (var file in dirinfo.GetFiles("*.h"))
            {
                if(file.Name == "fpdf_ext.h")
                    continue;

                module.Headers.Add(file.Name);
            }
        }

        public void SetupPasses(Driver driver)
        {
            driver.AddTranslationUnitPass(new FixCommentsPass());

        }
    }
}