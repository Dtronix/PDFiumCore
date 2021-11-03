using System;
using System.Threading.Tasks;
using PDFiumCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using RectangleF = System.Drawing.RectangleF;

namespace PDFiumCoreDemo
{
    public unsafe class PdfImage : IDisposable
    {
        private readonly FpdfBitmapT _pdfBitmap;
        private readonly UnmanagedMemoryManager<byte> _mgr;

        public int Width { get; }

        public int Height { get; }

        public int Stride { get; }

        public Image<Argb32> ImageData { get; }

        internal PdfImage(
            FpdfBitmapT pdfBitmap, 
            int width, 
            int height)
        {
            _pdfBitmap = pdfBitmap;
            var scan0 = fpdfview.FPDFBitmapGetBuffer(pdfBitmap);
            Stride = fpdfview.FPDFBitmapGetStride(pdfBitmap);
            Height = height;
            Width = width;
            _mgr = new UnmanagedMemoryManager<byte>((byte*)scan0, Stride * Height);

            ImageData = Image.WrapMemory<Argb32>(Configuration.Default, _mgr.Memory, width, height);
        }

        public void Dispose()
        {
            ImageData.Dispose();
            fpdfview.FPDFBitmapDestroy(_pdfBitmap);
        }
    }
}