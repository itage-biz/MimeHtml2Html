﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using NUglify;
using NUglify.Css;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Itage.MimeHtml2Html
{
    internal class DocumentPostprocessor
    {
        private static readonly char PaddingMarker = Encoding.UTF8.GetString(new byte[] {0xef, 0xbf, 0xbd})[0];
        private readonly MimeConversionOptions _options;
        private readonly IDocument _document;
        private readonly Uri _baseUri;
        private readonly IReadOnlyCollection<MimePartChunk> _chunks;
        private readonly ILogger _logger;

        internal DocumentPostprocessor(
            MimeConversionOptions options,
            IDocument document,
            Uri baseUri,
            IReadOnlyCollection<MimePartChunk> chunks, ILogger logger)
        {
            _options = options;
            _document = document;
            _baseUri = baseUri;
            _chunks = chunks;
            _logger = logger;
        }

        public IDocument Run()
        {
            ExpandUrlInStyles(_document.DocumentElement.GetElementsByTagName("style"));
            EmbedImgTags(_document.GetElementsByTagName("img"));
            EmbedExternalStyles(_document.DocumentElement
                .GetElementsByTagName("link")
                .Where(r => r.GetAttribute("rel") == "stylesheet"));
            RemoveScripts();
            return _document;
        }


        private void ExpandUrlInStyles(IEnumerable<IElement> styles)
        {
            foreach (IElement? styleTag in styles)
            {
                styleTag.InnerHtml = new Regex(@"url\((['""]?)(.*?)\1\)")
                    .Replace(styleTag.InnerHtml, match => EvaluateMatch(match, _baseUri));
            }
        }

        private void EmbedImgTags(IEnumerable<IElement> images)
        {
            foreach (var imageTag in images)
            {
                string src = imageTag.GetAttribute("src");
                if (string.IsNullOrWhiteSpace(src))
                {
                    continue;
                }
                var imgUri = new Uri(_baseUri, src);
                MimePartChunk? imgChunk = _chunks.FirstOrDefault(r => r.Location == imgUri);

                if (imgChunk == null || !imgChunk.MimeType.StartsWith("image"))
                {
                    continue;
                }

                string dataUrl = _options.CompressImages
                    ? CompressImage(imgChunk)
                    : imgChunk.AsDataUri();

                imageTag.SetAttribute("src", dataUrl);
            }
        }


        private void EmbedExternalStyles(IEnumerable<IElement> styles
        )
        {
            foreach (IElement styleTag in styles.ToList())
            {
                string styleUri = styleTag.GetAttribute("href");
                MimePartChunk? styleChunk =
                    _chunks.FirstOrDefault(r => r.Location != null && r.Location.AbsoluteUri == styleUri);
                if (styleChunk == null)
                {
                    _logger.LogWarning("Cannot find mime part for `{StyleUri}`", styleUri);
                    continue;
                }

                IElement style = _document.CreateElement("style");
                string cssText = styleChunk.AsText()
                    .Replace(PaddingMarker, ' ')
                    .Trim();
                cssText = new Regex(@"url\((['""]?)(.*?)\1\)").Replace(cssText,
                    match => EvaluateMatch(match, styleChunk.Location));
                style.InnerHtml = cssText;
                style.InnerHtml = style.InnerHtml.Replace(PaddingMarker, ' ').Trim();
                if (_options.CompressCss)
                {
                    style.InnerHtml = CompressCss(style.InnerHtml);
                }

                styleTag.Replace(style);
            }
        }

        private string CompressImage(MimePartChunk chunk)
        {
            IImageInfo? imageInfo = Image.Identify(chunk.Body);
            if (imageInfo == null)
            {
                _logger.LogError("Cannot determine image@{Location}; {Type}", chunk.Location, chunk.MimeType);
                return chunk.AsDataUri();
            }

            using Image image = Image.Load(chunk.Body);
            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms, new JpegEncoder
            {
                Quality = 50
            });
            return "data:image/jpeg;base64," + Convert.ToBase64String(ms.GetBuffer());
        }

        private string CompressCss(string style)
        {
            return Uglify.Css(style, new CssSettings
            {
                Indent = "", IgnoreAllErrors = true
            }).Code;
        }

        private void RemoveScripts()
        {
            foreach (var scriptTag in _document.GetElementsByTagName("script").ToList())
            {
                scriptTag.Remove();
            }

            foreach (var scriptTag in _document.QuerySelectorAll("link[rel='preload']").ToList())
            {
                scriptTag.Remove();
            }
        }

        private string EvaluateMatch(Match match, Uri baseUri)
        {
            if (baseUri.AbsoluteUri.StartsWith("cid:"))
            {
                baseUri = _baseUri;
            }

            if (match.Groups[2].Value.StartsWith("data:"))
            {
                return match.Groups[0].Value;
            }

            var uri = new Uri(baseUri, match.Groups[2].Value);

            MimePartChunk? firstMatchingChunk = _chunks.FirstOrDefault(c => c.Location == uri);
            if (firstMatchingChunk == null)
            {
                if (uri.AbsoluteUri.StartsWith("data:"))
                {
                    return match.Groups[0].Value;
                }

                if (uri.AbsoluteUri.StartsWith("http"))
                {
                    return "url('" + uri.AbsoluteUri + "')";
                }

                if (uri.AbsoluteUri.StartsWith("cid:"))
                {
                    var path = new Uri(_baseUri, uri.PathAndQuery);
                    _logger.LogDebug("Skipping cid:  {Uri1}+{Chunk}+{Uri2}={Uri3}", baseUri, match.Groups[2].Value,
                        uri.PathAndQuery,
                        path);
                    return match.Groups[0].Value;
                }

                _logger.LogDebug("Skipping {Uri}", uri);
                return match.Groups[0].Value;
            }

            _logger.LogDebug("Replacing {Uri}", uri);
            return "url('" + firstMatchingChunk.AsDataUri() + "')";
        }
    }
}