using SyaBot.Shared;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SyaBot.Client
{
    class Program
    {
        public static SemaphoreSlim Semaphore = new(0, 1);
        static readonly UdpClient client = new(40081, AddressFamily.InterNetwork) { EnableBroadcast = true };
        static async Task Main(string[] args)
        {
            ThreadPool.UnsafeQueueUserWorkItem(ReceiveThread, null);
            var message1 = JsonSerializer.SerializeToUtf8Bytes(new RegisterRequest("hello"));
            await client.SendAsync(message1, message1.Length, "255.255.255.255", 40080);

            var message2 = JsonSerializer.SerializeToUtf8Bytes(new TaskRequest("uri"));
            await client.SendAsync(message2, message2.Length, "255.255.255.255", 40080);
            await Semaphore.WaitAsync();
        }

        static async void ReceiveThread(object? state)
        {
            while (true)
            {
                var result = await client.ReceiveAsync();
                Console.WriteLine($"{result.RemoteEndPoint} says: {Encoding.UTF8.GetString(result.Buffer)}");
            }
        }
    }
}
