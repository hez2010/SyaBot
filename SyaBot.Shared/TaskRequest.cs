namespace SyaBot.Shared
{
    public class TaskRequest : SyaRequest
    {
        public TaskRequest(string uri)
        {
            Uri = uri;
        }

        public string Uri { get; }
    }
}
