using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Http
{
    public class HttpRequest
    {
        public HttpRequest(string url, HttpAccept httpAccept = null)
        {
            Method = HttpMethod.Get;
            Url = new HttpUri(url);
            Headers = new HttpHeader();
            ConnectionKeepAlive = true;
            AllowAutoRedirect = true;
            StoreRequestCookie = true;
            LogHttpError = true;
            Cookies = new Dictionary<string, string>();

            if (!RuntimeInfo.IsProduction)
            {
                AllowAutoRedirect = false;
            }

            if (httpAccept != null)
            {
                Headers.Accept = httpAccept.Value;
            }
        }

        public HttpUri Url { get; set; }
        public HttpMethod Method { get; set; }
        public HttpHeader Headers { get; set; }
        public byte[] ContentData { get; set; }
        public string ContentSummary { get; set; }
        public ICredentials Credentials { get; set; }
        public bool SuppressHttpError { get; set; }
        public IEnumerable<HttpStatusCode> SuppressHttpErrorStatusCodes { get; set; }
        public bool UseSimplifiedUserAgent { get; set; }
        public bool AllowAutoRedirect { get; set; }
        public bool ConnectionKeepAlive { get; set; }
        public bool LogResponseContent { get; set; }
        public bool LogHttpError { get; set; }
        public Dictionary<string, string> Cookies { get; private set; }
        public bool StoreRequestCookie { get; set; }
        public bool StoreResponseCookie { get; set; }
        public TimeSpan RequestTimeout { get; set; }

        /// <summary>
        /// When set together with ResponseStream, each successful read re-arms
        /// the timeout: a slow but moving download may take as long as it
        /// needs, while a stalled connection still dies after this long
        /// without data. RequestTimeout then only covers the pre-body phase.
        /// </summary>
        public TimeSpan IdleReadTimeout { get; set; }

        public TimeSpan RateLimit { get; set; }
        public string RateLimitKey { get; set; }
        public Stream ResponseStream { get; set; }

        public override string ToString()
        {
            return ToString();
        }

        public string ToString(bool includeMethod = true, bool includeSummary = true)
        {
            var builder = new StringBuilder();

            if (includeMethod)
            {
                builder.AppendFormat("Req: [{0}] ", Method);
            }

            builder.Append(Url);

            if (includeSummary && ContentSummary.IsNotNullOrWhiteSpace())
            {
                builder.Append(": ");
                builder.Append(ContentSummary);
            }

            return builder.ToString();
        }

        public void SetContent(byte[] data)
        {
            ContentData = data;
        }

        public void SetContent(string data)
        {
            var encoding = HttpHeader.GetEncodingFromContentType(Headers.ContentType);
            ContentData = encoding.GetBytes(data);
        }
    }
}
