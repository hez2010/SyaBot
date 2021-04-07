using System.Collections.Generic;
using System.Net;

namespace SyaBot.Client
{
    class DownloadBlockTask
    {
        public DownloadBlockTask(string uid, string uri, long offset, long length, Dictionary<string, string?>? headers, NetworkCredential? credential)
        {
            Uid = uid;
            Uri = uri;
            Offset = offset;
            Length = length;
            Headers = headers;
            Credential = credential;
        }

        public string Uid { get; }
        public string Uri { get; }
        public long Offset { get; }
        public long Length { get; }
        public Dictionary<string, string?>? Headers { get; }
        public NetworkCredential? Credential { get; }
    }
}
