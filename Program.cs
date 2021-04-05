using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyaBot
{
    class Program
    {
        public static SemaphoreSlim Semaphore = new(0, 1);
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: SyaBot [uri] [output_file]");
                Console.WriteLine("Example: SyaBot https://my.website.com/download.exe download.exe");
                return;
            }
            var dispatcher = new DownloadDispatcher();
            await dispatcher.DispatchTaskAsync(new(args[1], args[0], null, null), default);

            await Semaphore.WaitAsync();
        }
    }
}
