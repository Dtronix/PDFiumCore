using System;
using NUnit.Framework;

namespace PDFiumCore.Tests
{
    public class FpdfviewTests
    {
        private FpdfDocumentT _document;

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
            _document = fpdfview.FPDF_LoadDocument("pdf-sample.pdf", null);
        }

        [Test]
        public void ReadsPageCount()
        {
            Assert.AreEqual(1, fpdfview.FPDF_GetPageCount(_document));
        }
    }
}