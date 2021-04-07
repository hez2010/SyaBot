using System;

namespace SyaBot.Shared
{
    public class RequestModel<T> : RequestModel
    {
        public RequestModel() : base(typeof(T)) { }
        public virtual T? Data { get; set; }
    }

    public class RequestModel
    {
        public RequestModel() { }
        public RequestModel(Type type)
        {
            Type = type.ToString();
        }

        public string Type { get; init; } = default!;
        public string Id { get; } = Guid.NewGuid().ToString();
    }
}
