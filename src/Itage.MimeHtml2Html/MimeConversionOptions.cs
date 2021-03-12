namespace Itage.MimeHtml2Html
{
    /// <summary>
    /// Mime compression options
    /// </summary>
    public class MimeConversionOptions
    {
        /// <summary>
        /// CSS should be compressed 
        /// </summary>
        public bool CompressCss { get; set; } = true;

        /// <summary>
        /// Images should be compressed. PNG without any transparency will be saved as JPG
        /// </summary>
        public bool CompressImages { get; set; } = true;

        /// <summary>
        /// HTML should be minified.
        /// </summary>
        public bool CompressHtml { get; set; } = true;

        /// <summary>
        /// Save non-transparent PNGs as JPG
        /// </summary>
        public bool UglifyPng { get; set; } = true;

        /// <summary>
        /// Compression quality for JPEGs (And PNG)
        /// </summary>
        public int JpegCompressorQuality { get; set; } = 30;

        /// <summary>
        /// Max number of colors for PNG
        /// </summary>
        public int MaxPngColors { get; set; } = 256;
    }
}