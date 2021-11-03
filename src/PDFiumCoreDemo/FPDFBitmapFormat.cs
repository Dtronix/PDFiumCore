using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFiumCoreDemo
{
    [Flags]
    public enum FPDFBitmapFormat
    {
        /// <summary>
        /// Unknown or unsupported format.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Gray scale bitmap, one byte per pixel.
        /// </summary>
        Gray = 1,

        /// <summary>
        /// 3 bytes per pixel, byte order: blue, green, red.
        /// </summary>
        BGR = 2,

        /// <summary>
        /// 4 bytes per pixel, byte order: blue, green, red, unused.
        /// </summary>
        BGRx = 3,

        /// <summary>
        /// 4 bytes per pixel, byte order: blue, green, red, alpha.
        /// </summary>
        BGRA = 4
    }
}
