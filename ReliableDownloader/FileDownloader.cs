// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReliableDownloader.cs">
//   Copyright 2021 Sarabjot Singh. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ReliableDownloader
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File downloader for downloading files
    /// </summary>
    public class FileDownloader : IFileDownloader
    {
        /// <summary>
        /// Defining a batch size for dividing the files
        /// We should ideally read this from a config file
        /// Keeping it small to test the behavior
        /// We can decrease the batch size on network
        /// </summary>
        private const long BatchSize = 8192L;

        /// <summary>
        /// The web client for making the web requests
        /// </summary>
        private IWebSystemCalls reliableClient;

        /// <summary>
        /// Concurrent bag for all the cancellation tokens associated with download tasks
        /// Note: We can use a single cancellation token as well.
        /// This an idea to given more flexibility
        /// </summary>
        private ConcurrentBag<CancellationTokenSource> cancellationTokens = new ConcurrentBag<CancellationTokenSource>();

        /// <summary>
        /// Default Constructor
        /// </summary>
        public FileDownloader()
            : this(new ReliableWebSystemCalls())
        {
        }

        /// <summary>
        /// Constructor for Tests.
        /// TODO: Make this internal later.
        /// </summary>
        /// <param name="reliableClient"></param>
        public FileDownloader(IWebSystemCalls reliableClient)
        {
            this.reliableClient = reliableClient;
        }

        /// <inheritdoc>
        public Task<bool> DownloadFile(string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            cancellationTokens.Add(tokenSource);

            return new Task<bool>(() =>
            {
                var result = DownloadAsync(contentFileUrl, localFilePath, onProgressChanged, tokenSource.Token).ConfigureAwait(false);
                return true;
            }, tokenSource.Token);
        }

        private async Task DownloadAsync(string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged, CancellationToken cancellationToken)
        {
            var headerResponse = await reliableClient.GetHeadersAsync(contentFileUrl, cancellationToken).ConfigureAwait(false);

            if (headerResponse.Headers.AcceptRanges != null &&
                headerResponse.Headers.AcceptRanges.Contains("bytes"))
            {
                await DownloadPartialAsync(headerResponse, contentFileUrl, localFilePath, onProgressChanged, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await DownloadAsync(headerResponse, contentFileUrl, localFilePath, onProgressChanged, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task DownloadAsync(HttpResponseMessage headerResponse, string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged, CancellationToken cancellationToken)
        {
            if (File.Exists(localFilePath))
            {
                Console.WriteLine("Deleting existing file.");
                File.Delete(localFilePath);
            }

            long fileSize = headerResponse.Content.Headers.ContentLength ?? 0;
            var totalDownloaded = 0;

            using (Stream fileStream = File.OpenWrite(localFilePath))
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                var response = await reliableClient.DownloadContent(contentFileUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                    response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();

                var buffer = new byte[BatchSize];
                int bytesDownloaded;

                do
                {
                    bytesDownloaded = await stream.ReadAsync(buffer, 0, buffer.Length);
                    await fileStream.WriteAsync(buffer, 0, bytesDownloaded);
                    totalDownloaded += bytesDownloaded;

                    LogProgress(onProgressChanged, fileSize, totalDownloaded, 0, stopwatch.ElapsedMilliseconds);
                }
                while (bytesDownloaded > 0);
            }
        }

        /// <summary>
        /// Download the file partially
        /// </summary>
        /// <param name="headerResponse"></param>
        /// <param name="contentFileUrl"></param>
        /// <param name="localFilePath"></param>
        /// <param name="onProgressChanged"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task DownloadPartialAsync(HttpResponseMessage headerResponse, string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged, CancellationToken cancellationToken)
        {
            // Get the existing length of the file if already present
            // This could be because we cancelled the download in a previous run

            long existingLength = 0;
            if (File.Exists(localFilePath))
            {
                existingLength = new FileInfo(localFilePath).Length;

                // Delete existing file if the length is greater than the content length
                // This means the file is corrupted Or different file with the same name.
                if (headerResponse.Content.Headers != null &&
                   existingLength > Convert.ToInt64(headerResponse.Content.Headers.ContentLength))
                {
                    Console.WriteLine("Deleting existing file because it is invalid.");
                    File.Delete(localFilePath);
                    existingLength = 0;
                }
            }

            long fileSize = headerResponse.Content.Headers.ContentLength ?? 0;
            long totalDownloaded = existingLength;

            using (Stream fileStream = File.OpenWrite(localFilePath))
            {
                int batchCount = (int)Math.Ceiling(1.0 * (fileSize - existingLength) / BatchSize);

                fileStream.Position = existingLength;

                // This makes sure we resume where we left
                long from = existingLength;
                long to = from + BatchSize;

                Stopwatch stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < batchCount; i++)
                {
                    var response = await this.reliableClient.DownloadPartialContent(contentFileUrl, from, to, cancellationToken);

                    if (response.IsSuccessStatusCode)
                        response.EnsureSuccessStatusCode();
                    Stream stream = await response.Content.ReadAsStreamAsync();

                    byte[] buffer = new byte[BatchSize];
                    int bytesDownloaded;

                    do
                    {
                        bytesDownloaded = await stream.ReadAsync(buffer, 0, buffer.Length);
                        await fileStream.WriteAsync(buffer, 0, bytesDownloaded);
                        totalDownloaded += bytesDownloaded;

                        LogProgress(onProgressChanged, fileSize, totalDownloaded, existingLength, stopwatch.ElapsedMilliseconds);
                    }
                    while (bytesDownloaded > 0);

                    from = to + 1;
                    to += BatchSize;
                }
            }
        }

        /// <summary>
        /// Logs the progress informaton
        /// </summary>
        /// <param name="onProgressChanged">Action which logs progress</param>
        /// <param name="fileSize">The total file size</param>
        /// <param name="totalDownloaded">The bytes downloaded so far</param>
        /// <param name="elapsedTimeInMs">The elapsed time in ms</param>
        private static void LogProgress(Action<FileProgress> onProgressChanged, long fileSize, long totalDownloaded, long existingLength, long elapsedTimeInMs)
        {
            // Calculate the percentage based on the current downloaded and total file size
            var percentage = (totalDownloaded * 100 / fileSize);

            // Calculate remaining time based on time taken and progress so far
            int estimatedRemainingTime = (int)Math.Ceiling((double)(elapsedTimeInMs * fileSize) / (totalDownloaded - existingLength) - elapsedTimeInMs);

            // Logging the the progress
            onProgressChanged(new FileProgress(fileSize, totalDownloaded, percentage, new TimeSpan(0, 0, 0, 0, estimatedRemainingTime)));
        }

        /// <summary>
        /// Cancels all the downloads
        /// We can customize the cancel for individual downloads
        /// Invest a download id, and have a dictionary for caching individual tokens
        /// And then cancel which ever is required to be cancelled.
        /// </summary>
        public void CancelDownloads()
        {
            foreach (CancellationTokenSource tokenSource in cancellationTokens)
            {
                tokenSource.Cancel();
            }
        }
    }
}