namespace SyaBot.Shared
{
    public class TaskRequest : RequestModel<TaskRequest>
    {
        public TaskRequest(string uri)
        {
            Uri = uri;
        }

        public string Uri { get; }
    }
}
