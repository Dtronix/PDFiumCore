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

        public PDFiumCoreLibrary(string directoryName)
        {
            _directoryName = directoryName;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
            // Fix for generating code which will not compile.
            var fpdfLibraryConfig = ctx.FindClass("FPDF_LIBRARY_CONFIG_");
            fpdfLibraryConfig.First().Properties.First(f => f.OriginalName == "m_pUserFontPaths").Ignore = true;
        }


        public override void Setup(Driver driver)
        {
            var includeDirectory = Path.Combine(_directoryName, "include");
            driver.ParserOptions.SetupMSVC(VisualStudioVersion.Latest);
            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            //options.Verbose = true;
            options.CommentKind = CommentKind.BCPLSlash;

            var module = options.AddModule("PDFiumCore");
            module.SharedLibraryName = "pdfium";
   
            // Ensure that the win32 includes are ignored.
            module.Undefines.Add("_WIN32");

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

        public override void SetupPasses(Driver driver)
        {
            driver.AddTranslationUnitPass(new FixCommentsPass());

        }
    }
}