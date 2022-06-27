using System;
using System.IO;
using NUnit.Framework;

namespace PDFiumCore.Tests
{
    public class FpdfviewTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            fpdfview.FPDF_InitLibrary();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            fpdfview.FPDF_DestroyLibrary();
        }

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void ReadsPageCount()
        {
            var document = fpdfview.FPDF_LoadDocument("pdf-sample.pdf", null);
            Assert.AreEqual(1, fpdfview.FPDF_GetPageCount(document));
        }

        [Test]
        public unsafe void FPDF_LoadMemDocument()
        {
            using var fs = File.OpenRead("pdf-sample.pdf");
            var fileBytes = new byte[fs.Length];
            using var ms = new MemoryStream(fileBytes);

            // Copy file the underlying byte stream.
            fs.CopyTo(ms);

            fixed (void* ptr = fileBytes)
            {
                var document = fpdfview.FPDF_LoadMemDocument(new IntPtr(ptr), fileBytes.Length, null);
                Assert.AreEqual(1, fpdfview.FPDF_GetPageCount(document));
            }
        }
    }
}