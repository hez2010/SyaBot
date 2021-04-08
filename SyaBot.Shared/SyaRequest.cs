using System;

namespace SyaBot.Shared
{
    public class SyaRequest
    {
        public SyaRequest()
        {
            Type = GetType().ToString();
            Id = Guid.NewGuid().ToString();
        }

        public string Type { get; init; }
        public string Id { get; init; }
    }
}
