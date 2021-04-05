using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyaBot
{
    class DownloadWorker
    {
        private readonly static ConcurrentDictionary<DownloadBlockTask, (DownloadContext Context, FileStream Stream)> contexts = new();
        private readonly static SemaphoreSlim semaphore = new(8, 8);

        public event Func<DownloadContext, Task>? OnBlockCompleted;

        public void QueueDownloadTask(DownloadTask task, DownloadBlockTask blockTask, CancellationToken token)
        {
            var context = new DownloadContext(task, blockTask, semaphore, token);
            context.OnBlockReceived += OnBlockReceived;

            contexts[blockTask] = (context, new FileStream($"{blockTask.Uid}_{blockTask.Offset}_{blockTask.Length}.block", FileMode.Create));
            context.Start();
        }

        private async Task OnBlockReceived(DownloadContext context, byte[] block)
        {
            if (block.Length == 0)
            {
                await contexts[context.BlockTask].Stream.FlushAsync();
                await contexts[context.BlockTask].Stream.DisposeAsync();
                contexts.TryRemove(context.BlockTask, out _);

                Console.WriteLine($"[Worker #{context.WorkerId}] Task completed");

                if (OnBlockCompleted is not null)
                {
                    await OnBlockCompleted.Invoke(context);
                }
            }
            else
            {
                await contexts[context.BlockTask].Stream.WriteAsync(block);
            }
        }
    }
}
