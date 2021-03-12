using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Itage.MimeHtml2Html
{
    /// <summary>
    /// Mime converter class
    /// </summary>
    public class MimeConverter
    {
        private readonly ILogger _logger;
        private readonly MimeConversionOptions _options;

        /// <param name="options">Conversion options</param>
        /// <param name="logger">Optional logger</param>
        public MimeConverter(MimeConversionOptions? options = null, ILogger? logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _options = options ?? new MimeConversionOptions();
        }

        /// <summary>
        /// Converts file
        /// </summary>
        /// <param name="sourceFilename">Source MHT file name</param>
        /// <param name="destinationFilename">Destination HTML file name </param>
        /// <returns>If operation was successful</returns>
        public async Task<bool> ConvertFile(string sourceFilename, string destinationFilename)
        {
            using Stream sourceStream = File.OpenRead(sourceFilename);
            using Stream destinationStream = File.OpenWrite(destinationFilename);
            return await Convert(sourceStream, destinationStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Converts stream
        /// </summary>
        /// <param name="sourceStream">Source MHT stream</param>
        /// <param name="destinationStream">Destination HTML stream</param>
        /// <returns>If operation was successful</returns>
        public async Task<bool> Convert(Stream sourceStream, Stream destinationStream)
        {
            using var ms = new MemoryStream();
            await sourceStream.CopyToAsync(ms);

            byte[]? result = await Convert(ms.GetBuffer());
            ms.Dispose();

            if (result == null)
            {
                return false;
            }

            await destinationStream.WriteAsync(result, 0, result.Length);
            return true;
        }

        /// <summary>
        /// Converts MHG byte array to destination 
        /// </summary>
        /// <param name="mhtmlData">MHT data</param>
        /// <returns>Resulting byte array if operation was successful; null otherwise</returns>
        public async Task<byte[]?> Convert(byte[] mhtmlData)
        {
            var converter = new MhtmlParser(_options, _logger);
            return await converter.ToHtml(mhtmlData, CancellationToken.None);
        }
    }
}