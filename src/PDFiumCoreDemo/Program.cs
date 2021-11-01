using System;
using PDFiumCore;

namespace PDFiumCoreDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var document = fpdfview.FPDF_LoadDocument("pdf-sample.pdf", null);
            var pages = fpdfview.FPDF_GetPageCount(document);

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
