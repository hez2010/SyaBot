using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SyaBot
{
    class DownloadContext
    {
        private static int counter = 0;
        public DownloadContext(DownloadTask task, DownloadBlockTask blockTask, SemaphoreSlim semaphore, CancellationToken token)
        {
            Task = task;
            BlockTask = blockTask;
            Semaphore = semaphore;
            Token = token;

            WorkerId = counter++;
        }

        public void Start()
        {
            ThreadPool.UnsafeQueueUserWorkItem(WorkerThread, this, false);
        }

        private SemaphoreSlim Semaphore { get; }

        private CancellationToken Token { get; }
        public DownloadTask Task { get; }
        public DownloadBlockTask BlockTask { get; }

        public event Func<DownloadContext, byte[], Task>? OnBlockReceived;
        public int WorkerId { get; private set; }

        private static async void WorkerThread(DownloadContext context)
        {
            await context.Semaphore.WaitAsync(context.Token);

            Console.WriteLine($"[Worker #{context.WorkerId}] Start block download task: from = {context.BlockTask.Offset}, length = {context.BlockTask.Length}");

            try
            {
                using var client = new HttpClient();

                using var request = new HttpRequestMessage(HttpMethod.Get, context.BlockTask.Uri);

                if (context.BlockTask.Headers is not null)
                {
                    foreach (var header in context.BlockTask.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                var bytesRead = 0;
                var retryCount = 0;

                while (retryCount < 3)
                {
                    request.Headers.Range = new(context.BlockTask.Offset + bytesRead, context.BlockTask.Offset + context.BlockTask.Length - 1);

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.Token);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (retryCount < 3)
                        {
                            Console.WriteLine($"[Worker #{context.WorkerId}] Failed to retrieve header, retrying...");
                            retryCount++;
                            continue;
                        }
                        else
                        {
                            throw new Exception($"An error occurred with response status code {response.StatusCode}");
                        }
                    }

                    using var stream = await response.Content.ReadAsStreamAsync();

                    var block = new Memory<byte>(new byte[1048576]);
                    var length = -1;

                    while (length != 0)
                    {
                        try
                        {
                            length = await stream.ReadAsync(block, context.Token);
                        }
                        catch
                        {
                            if (retryCount < 3)
                            {
                                Console.WriteLine($"[Worker #{context.WorkerId}] Failed to retrieve data, retrying...");
                                retryCount++;
                                continue;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        bytesRead += length;

                        if (context.OnBlockReceived is not null)
                        {
                            await context.OnBlockReceived.Invoke(context, block[..length].ToArray());
                        }
                    }

                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker #{context.WorkerId}] Failed: {ex}");
                Console.WriteLine("Exit");
                Environment.Exit(-1);
            }
            finally
            {
                context.Semaphore.Release();
            }
        }
    }
}
