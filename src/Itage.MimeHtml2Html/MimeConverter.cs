using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Itage.MimeHtml2Html
{
    public class MimeConverter
    {
        private readonly ILogger _logger;
        private readonly MimeConversionOptions _options;

        public MimeConverter(ILogger logger, MimeConversionOptions? options = null)
        {
            _logger = logger;
            _options = options ?? new MimeConversionOptions();
        }

        public async Task<bool> ConvertFile(string sourceFilename, string destinationFilename)
        {
            using Stream sourceStream = File.OpenRead(sourceFilename);
            using Stream destinationStream = File.OpenWrite(destinationFilename);
            return await Convert(sourceStream, destinationStream).ConfigureAwait(false);
        }

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

        public async Task<byte[]?> Convert(byte[] mhtmlData)
        {
            var converter = new MhtmlParser(_options, _logger);
            return await converter.ToHtml(mhtmlData, CancellationToken.None);
        }
    }
}
