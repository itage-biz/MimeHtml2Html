using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Itage.MimeHtml2Html
{
    public class MimeConverter
    {
        private readonly ILogger<MimeConverter> _logger;
        private readonly MimeConversionOptions _options;

        public MimeConverter(ILogger<MimeConverter> logger, MimeConversionOptions? options = null)
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

            string? result = await Convert(ms.GetBuffer());
            ms.Dispose();
            if (result == null)
            {
                return false;
            }

            byte[] convertedData = Encoding.UTF8.GetBytes(result);
            await destinationStream.WriteAsync(convertedData, 0, convertedData.Length);
            return true;
        }

        public async Task<string?> Convert(byte[] mhtmlData)
        {
            var converter = new MhtmlParser(_options, _logger);
            return await converter.ToHtml(mhtmlData, CancellationToken.None);
        }
    }
}