using System;

namespace PDFiumCore
{
    [Flags]
    public enum RenderFlags
    {
        /// <summary>
        /// FPDF_ANNOT: Set if annotations are to be rendered.
        /// </summary>
        RenderAnnotations = 0x01,

        /// <summary>
        /// FPDF_LCD_TEXT: Set if using text rendering optimized for LCD display. This flag will only take effect if anti-aliasing is enabled for text.
        /// </summary>
        OptimizeTextForLcd = 0x02,

        /// <summary>
        /// FPDF_NO_NATIVETEXT: Don't use the native text output available on some platforms
        /// </summary>
        NoNativeText = 0x04,

        /// <summary>
        /// FPDF_GRAYSCALE: Grayscale output
        /// </summary>
        Grayscale = 0x08,

        /// <summary>
        /// // FPDF_RENDER_LIMITEDIMAGECACHE: Limit image cache size
        /// </summary>
        LimitImageCacheSize = 0x200, 

        /// <summary>
        /// FPDF_RENDER_FORCEHALFTONE: Always use halftone for image stretching
        /// </summary>
        ForceHalftone = 0x400,

        /// <summary>
        /// FPDF_PRINTING: Render for printing
        /// </summary>
        RenderForPrinting = 0x800,

        /// <summary>
        /// FPDF_RENDER_NO_SMOOTHTEXT: Set to disable anti-aliasing on text. This flag will also disable LCD optimization for text rendering
        /// </summary>
        DisableTextAntialiasing = 0x1000,


        /// <summary>
        /// FPDF_RENDER_NO_SMOOTHIMAGE: Set to disable anti-aliasing on images.
        /// </summary>
        DisableImageAntialiasing = 0x2000,

        /// <summary>
        /// FPDF_RENDER_NO_SMOOTHPATH: Set to disable anti-aliasing on paths.
        /// </summary>
        DisablePathAntialiasing = 0x4000
    }
}