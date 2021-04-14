using System;
using System.Text.Json.Serialization;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Dummy class so C# 'Record' types can compile on NetStandard and NetFx.
    /// </summary>
    public sealed class IsExternalInit { }
}

namespace SyaBot.Shared
{
    public class SyaMessage
    {
        [JsonConstructor]
        public SyaMessage()
        {
            Type = GetType().ToString();
            Id = Guid.NewGuid().ToString();
        }

        public SyaMessage(string id)
        {
            Type = GetType().ToString();
            Id = id;
        }

        public string? Type { get; init; }
        public string Id { get; init; }
    }

    public class SyaMessage<T> : SyaMessage
    {
        [JsonConstructor]
        public SyaMessage() : base() { }

        public SyaMessage(string id) : base(id) { }

        public T? Data { get; set; }
    }
}
