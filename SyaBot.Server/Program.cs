using SyaBot.Shared;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SyaBot.Server
{
    class UdpContext<T> where T : SyaMessage
    {

        public UdpContext(MethodInfo method, IPEndPoint? remote, T data, string id)
        {
            Method = method;
            RemoteEndPoint = remote;
            Data = data;
            RequestId = id;
        }

        public IPEndPoint? RemoteEndPoint { get; }

        public T Data { get; }

        public MethodInfo Method { get; }

        public string RequestId { get; }

        public SyaMessage<U> CreateResponse<U>(U data)
        {
            return new SyaMessage<U> { Id = RequestId, Type = null, Data = data };
        }
    }

    class Program
    {
        static readonly UdpClient client = new(40080, AddressFamily.InterNetwork);
        static readonly ConcurrentDictionary<string, (Type ModelType, Func<object, object> Handler, MethodInfo Method, bool Awaitable, PropertyInfo? GetResult)> handlers = new();

        static object? CreateUdpContext(Type dataType, MethodInfo method, IPEndPoint? remote, object data, string id)
        {
            return Activator.CreateInstance(typeof(UdpContext<>).MakeGenericType(dataType), method, remote, data, id);
        }

        static void AddHandler<T, U>(Func<UdpContext<T>, U> handler) where T : SyaMessage where U : SyaMessage
        {
            handlers[typeof(T).ToString()] = (typeof(T), d => handler((UdpContext<T>)d), handler.Method, false, null);
        }

        static void AddHandler<T, U>(Func<UdpContext<T>, Task<U>> handler) where T : SyaMessage where U : SyaMessage
        {
            handlers[typeof(T).ToString()] = (typeof(T), d => handler((UdpContext<T>)d), handler.Method, true, typeof(Task<U>).GetProperty("Result"));
        }

        static void AddHandler<T, U>(Func<UdpContext<T>, ValueTask<U>> handler) where T : SyaMessage where U : SyaMessage
        {
            handlers[typeof(T).ToString()] = (typeof(T), d => handler((UdpContext<T>)d).AsTask(), handler.Method, true, typeof(Task<U>).GetProperty("Result"));
        }

        static async Task Main(string[] args)
        {
            AddHandler<RegisterRequest, SyaMessage<string>>(RegisterHandler);
            AddHandler<TaskRequest, SyaMessage>(TaskHandler);

            while (true)
            {
                var result = await client.ReceiveAsync();
                Console.WriteLine($"Raw request string: {Encoding.UTF8.GetString(result.Buffer)}");
                var request = JsonSerializer.Deserialize<SyaMessage>(result.Buffer);
                if (request is null) continue;
                Console.WriteLine($"Request id: {request.Id}, type: {request.Type}");

                if (request.Type is null || !handlers.ContainsKey(request.Type)) continue;
                var (type, handler, method, awaitable, getResult) = handlers[request.Type];

                var data = JsonSerializer.Deserialize(result.Buffer, type);
                if (data is null) continue;

                Console.WriteLine($"Map handler: {method}");

                var context = CreateUdpContext(type, method, result.RemoteEndPoint, data, request.Id);
                if (context is null) continue;

                ThreadPool.UnsafeQueueUserWorkItem<object?>(async o =>
                {
                    var handlerResult = handler(context);
                    if (awaitable)
                    {
                        var task = (Task)handlerResult;
                        await task.ConfigureAwait(false);
                        if (getResult is null)
                        {
                            handlerResult = null;
                        }
                        else
                        {
                            handlerResult = getResult.GetValue(handlerResult);
                        }
                    }

                    var reponse = JsonSerializer.SerializeToUtf8Bytes(handlerResult);
                    await client.SendAsync(reponse, reponse.Length, result.RemoteEndPoint);
                }, null, false);
            }
        }

        static async Task<SyaMessage<string>> RegisterHandler(UdpContext<RegisterRequest> model)
        {
            Console.WriteLine($"Name = {model.Data.Name}");
            await Task.Delay(100);
            return model.CreateResponse("register");
        }

        static SyaMessage<string> TaskHandler(UdpContext<TaskRequest> model)
        {
            Console.WriteLine($"Uri = {model.Data.Uri}");
            return model.CreateResponse("task");
        }
    }
}
