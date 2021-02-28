using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Itage.MimeHtml2Html
{
    internal class MhtmlBodyLoader : IDisposable
    {
        private const string CharsetMarker = "charset=";
        private const string MetaQuerySelector = "meta[http-equiv='Content-Type']['content']";
        private readonly ILogger _logger;
        private readonly IBrowsingContext _browsingContext;

        public MhtmlBodyLoader(ILogger logger, IConfiguration? configuration = null)
        {
            _logger = logger;
            _browsingContext = BrowsingContext.New(configuration ?? new Configuration());
        }

        public async Task<IDocument?> Load(MimeEntity entity, CancellationToken cancellationToken = new())
        {
            string? charset = entity.ContentType.Charset;

            if (entity is not MultipartRelated multipartRelated)
            {
                _logger.LogError("Message is not in MHT format");
                return null;
            }

            if (multipartRelated.Root is not TextPart textPart)
            {
                _logger.LogError("Root element is not TextPart");
                return null;
            }

            IDocument? body = await LoadDocument(textPart.GetText(Encoding.UTF8), cancellationToken);

            if (body == null)
            {
                _logger.LogError("Cannot retrieve body");
                return null;
            }

            IElement? contentTypeMeta = body.QuerySelector(MetaQuerySelector);

            string? contentType = contentTypeMeta?.GetAttribute("content");

            if (contentType != null && contentType.Contains(CharsetMarker))
            {
                charset = contentType.Substring(
                    contentType.LastIndexOf(CharsetMarker, StringComparison.InvariantCulture) +
                    CharsetMarker.Length);
            }

            if (charset == null)
            {
                return body;
            }

            var encoding = Encoding.GetEncoding(charset);
            string text = textPart.GetText(encoding);
            text = RemoveEncoding(text);
            body = await LoadDocument(text, cancellationToken);
            return body;
        }

        private async Task<IDocument?> LoadDocument(string body, CancellationToken cancellationToken = new())
        {
            IDocument? document = await _browsingContext.OpenAsync(req => req.Content(body), cancellationToken);
            if (document == null) return document;

            // Create meta http-equiv tag
            IElement meta = document.CreateElement("meta");
            meta.SetAttribute("http-equiv", "Content-Type");
            meta.SetAttribute("content", "text/html; charset=UTF8");
            document.Head.AppendChild(meta);

            return document;
        }

        private static string RemoveEncoding(string text)
        {
            return new Regex(@"<meta[^<>]+http-equiv[^<>]+>").Replace(text, "");
        }

        public void Dispose()
        {
            _browsingContext.Dispose();
        }
    }
}