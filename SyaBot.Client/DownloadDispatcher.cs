using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SyaBot.Client
{
    class DownloadDispatcher
    {
        private const int WorkerCount = 16;
        private readonly static ConcurrentDictionary<DownloadTask, (FileStream Stream, SemaphoreSlim Semaphore)> files = new();

        public async Task DispatchTaskAsync(DownloadTask task, CancellationToken token)
        {
            Console.WriteLine($"Dispatch task: {task.Uid}");
            using var client = new HttpClient();

            using var request = new HttpRequestMessage(HttpMethod.Get, task.Uri);

            if (task.Headers is not null)
            {
                foreach (var header in task.Headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed");
                return;
            }

            var length = response.Content.Headers.ContentLength;

            if (length is null)
            {
                Console.WriteLine("Unsupported task");
                return;
            }
            else
            {
                Console.WriteLine($"Length = {length}");
            }

            files[task] = (new(task.Uid, FileMode.Create), new(1, 1));

            var worker = new DownloadWorker();
            worker.OnBlockCompleted += OnBlockCompleted;

            long offset = 0;

            for (var i = 0; i < WorkerCount - 1; i++)
            {
                worker.QueueDownloadTask(task, new(task.Uid, task.Uri, offset, length.Value / WorkerCount, task.Headers, task.Credential), token);
                offset += length.Value / WorkerCount;
            }

            worker.QueueDownloadTask(task, new(task.Uid, task.Uri, offset, length.Value - offset, task.Headers, task.Credential), token);
        }

        private async Task OnBlockCompleted(DownloadContext context)
        {
            var semaphore = files[context.Task].Semaphore;
            await semaphore.WaitAsync();

            try
            {
                files[context.Task].Stream.Seek(context.BlockTask.Offset, SeekOrigin.Begin);
                await using (var block = new FileStream($"{context.BlockTask.Uid}_{context.BlockTask.Offset}_{context.BlockTask.Length}.block", FileMode.Open))
                {
                    await block.CopyToAsync(files[context.Task].Stream);
                }

                File.Delete($"{context.BlockTask.Uid}_{context.BlockTask.Offset}_{context.BlockTask.Length}.block");

                context.Task.CompletedCount++;

                if (context.Task.CompletedCount == WorkerCount)
                {
                    Console.WriteLine("Download completed");

                    await files[context.Task].Stream.FlushAsync();
                    await files[context.Task].Stream.DisposeAsync();

                    files.TryRemove(context.Task, out _);

                    Program.Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                semaphore.Release();
            }

        }
    }
}
