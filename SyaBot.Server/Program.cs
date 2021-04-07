using SyaBot.Shared;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SyaBot.Server
{
    class UdpContext<T> where T : RequestModel
    {
        public UdpContext(IPEndPoint? remote, T data)
        {
            RemoteEndPoint = remote;
            Data = data;
        }

        public IPEndPoint? RemoteEndPoint { get; }

        public T Data { get; }
    }

    class Program
    {
        static readonly UdpClient client = new(40080, AddressFamily.InterNetwork);
        static readonly ConcurrentDictionary<string, (Type ModelType, Func<object, Task> Handler)> handlers = new();

        static object? CreateUdpContext(Type dataType, IPEndPoint? remote, object data)
        {
            return Activator.CreateInstance(typeof(UdpContext<>).MakeGenericType(dataType), remote, data);
        }

        static void AddHandler<T>(Func<UdpContext<T>, Task> handler) where T : RequestModel
        {
            handlers[typeof(T).ToString()] = (typeof(T), d => handler((UdpContext<T>)d));
        }

        static async Task Main(string[] args)
        {
            AddHandler<RegisterRequest>(RegisterHandler);
            AddHandler<TaskRequest>(TaskHandler);

            while (true)
            {
                var result = await client.ReceiveAsync();
                Console.WriteLine(Encoding.UTF8.GetString(result.Buffer));
                var request = JsonSerializer.Deserialize<RequestModel>(result.Buffer);
                if (request is null) continue;
                Console.WriteLine(request.Type);

                if (!handlers.ContainsKey(request.Type)) continue;
                var (type, handler) = handlers[request.Type];

                var data = JsonSerializer.Deserialize(result.Buffer, type);
                if (data is null) continue;

                Console.WriteLine($"Map handler: {type}");

                var context = CreateUdpContext(type, result.RemoteEndPoint, data);
                if (context is null) continue;

                await handler(context);
            }
        }

        static Task RegisterHandler(UdpContext<RegisterRequest> model)
        {
            Console.WriteLine($"Name = {model.Data.Name}");
            return Task.CompletedTask;
        }

        static Task TaskHandler(UdpContext<TaskRequest> model)
        {
            Console.WriteLine($"Uri = {model.Data.Uri}");
            return Task.CompletedTask;
        }
    }
}
