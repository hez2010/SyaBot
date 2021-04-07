using System.Collections.Generic;
using System.Net;

namespace SyaBot.Client
{
    class DownloadTask
    {
        public DownloadTask(string uid, string uri, Dictionary<string, string?>? headers, NetworkCredential? credential)
        {
            Uid = uid;
            Uri = uri;
            Headers = headers;
            Credential = credential;
        }

        public int CompletedCount { get; set; }
        public string Uid { get; }
        public string Uri { get; }
        public Dictionary<string, string?>? Headers { get; }
        public NetworkCredential? Credential { get; }
    }
}
