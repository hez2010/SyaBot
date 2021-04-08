using SyaBot.Shared;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SyaBot.Server
{
    class UdpContext<T> where T : SyaRequest
    {
        public UdpContext(MethodInfo method, IPEndPoint? remote, T data)
        {
            Method = method;
            RemoteEndPoint = remote;
            Data = data;
        }

        public IPEndPoint? RemoteEndPoint { get; }

        public T Data { get; }

        public MethodInfo Method { get; }
    }

    class Program
    {
        static readonly UdpClient client = new(40080, AddressFamily.InterNetwork);
        static readonly ConcurrentDictionary<string, (Type ModelType, Func<object, Task> Handler, MethodInfo Method)> handlers = new();

        static object? CreateUdpContext(Type dataType, MethodInfo method, IPEndPoint? remote, object data)
        {
            return Activator.CreateInstance(typeof(UdpContext<>).MakeGenericType(dataType), method, remote, data);
        }

        static void AddHandler<T>(Func<UdpContext<T>, Task> handler) where T : SyaRequest
        {
            handlers[typeof(T).ToString()] = (typeof(T), d => handler((UdpContext<T>)d), handler.Method);
        }

        static async Task Main(string[] args)
        {
            AddHandler<RegisterRequest>(RegisterHandler);
            AddHandler<TaskRequest>(TaskHandler);

            while (true)
            {
                var result = await client.ReceiveAsync();
                Console.WriteLine($"Raw request string: {Encoding.UTF8.GetString(result.Buffer)}");
                var request = JsonSerializer.Deserialize<SyaRequest>(result.Buffer);
                if (request is null) continue;
                Console.WriteLine($"Request id: {request.Id}, type: {request.Type}");

                if (!handlers.ContainsKey(request.Type)) continue;
                var (type, handler, method) = handlers[request.Type];

                var data = JsonSerializer.Deserialize(result.Buffer, type);
                if (data is null) continue;

                Console.WriteLine($"Map handler: {method}");

                var context = CreateUdpContext(type, method, result.RemoteEndPoint, data);
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
