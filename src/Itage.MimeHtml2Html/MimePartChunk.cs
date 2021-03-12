using System;
using System.Text;

namespace Itage.MimeHtml2Html
{
    internal class MimePartChunk
    {
        public string MimeType { get; }
        public Uri Location { get; }
        public byte[] Body { get; }

        public MimePartChunk(string mimeType, Uri location, byte[] body)
        {
            MimeType = mimeType;
            Location = location;
            Body = body;
        }

        public string AsText()
        {
            return Encoding.UTF8.GetString(Body);
        }

        public string AsDataUri()
        {
            return "data:" + MimeType + ";base64," + Convert.ToBase64String(Body);
        }

        public byte[] AsByteArray() => Body;
    }
}