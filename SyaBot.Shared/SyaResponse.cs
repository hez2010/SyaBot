namespace SyaBot.Shared
{
    public class SyaResponse
    {
        public SyaResponse() { }
        public SyaResponse(string id)
        {
            Id = id;
        }
        public string Id { get; set; } = default!;
    }
}
