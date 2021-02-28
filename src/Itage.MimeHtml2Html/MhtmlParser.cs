using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Itage.MimeHtml2Html
{
    /// <summary>
    /// HTMLParser is an object that can decode mhtml into ASCII text.
    /// Using getHTMLText() will generate static HTML with inline images. 
    /// </summary>
    internal class MhtmlParser
    {
        private readonly MimeConversionOptions _options;
        private readonly ILogger _logger;

        public MhtmlParser(MimeConversionOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<string?> ToHtml(byte[] contents, CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream(contents);
            MimeMessage? message = await MimeMessage.LoadAsync(ms, cancellationToken);
            if (message == null)
            {
                return null;
            }

            List<MimePartChunk> chunks = message.BodyParts.OfType<MimePart>()
                .Select(part =>
                {
                    using var outStream = new MemoryStream();
                    part.Content.DecodeTo(outStream, cancellationToken);
                    return new MimePartChunk(part.ContentType.MimeType, part.ContentLocation, outStream.GetBuffer());
                })
                .ToList();
            Uri baseUri = message.Body.ContentLocation ?? new Uri("http://localhost/");
            using var bodyLoader = new MhtmlBodyLoader(_logger);

            IDocument? doc = await bodyLoader.Load(message.Body, cancellationToken);

            if (doc == null)
            {
                return null;
            }

            var processor = new DocumentPostprocessor(_options, doc, baseUri, chunks, _logger);
            doc = processor.Run();

            return doc.DocumentElement.ToHtml(new MinifyMarkupFormatter());
        }
    }
}