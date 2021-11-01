using NUnit.Framework;

namespace PDFiumCore.Tests
{
    public class GeneralTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var document = fpdfview.FPDF_LoadDocument("pdf-sample.pdf", null);
            Assert.AreEqual(1, fpdfview.FPDF_GetPageCount(document));
        }
    }
}