// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReliableWebSystemCalls.cs">
//   Copyright 2021 Sarabjot Singh. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ReliableDownloader
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Reliable client for web calls
    /// </summary>
    public class ReliableWebSystemCalls : IWebSystemCalls
    {
        /// <summary>
        /// The web client for making the web requests
        /// </summary>
        private IWebSystemCalls webSystemCalls;

        /// <summary>
        /// Gets or sets the default wait
        /// </summary>
        public TimeSpan DefaultWait { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets the default wait after 2 retries
        /// </summary>
        public TimeSpan DefaultWaitAfterTwoRetries { get; set; } = TimeSpan.FromSeconds(60 * 2);

        /// <summary>
        /// Gets or sets the delay function to call for waiting between retries.
        /// Settable so unit tests can override it.
        /// </summary>
        public Func<TimeSpan, Task> Delay { get; set; } = Task.Delay;

        /// <summary>
        /// Default constructor for the reliable web system calls
        /// </summary>
        public ReliableWebSystemCalls()
            : this(new WebSystemCalls())
        {
        }

        /// <summary>
        /// USED FOR TESTING
        /// </summary>
        /// <param name="webSystemCalls"></param>
        public ReliableWebSystemCalls(IWebSystemCalls webSystemCalls)
        {
            this.webSystemCalls = webSystemCalls;
        }

        /// <summary>
        /// Call DownloadContent with retries
        /// </summary>
        /// <param name="url">The url</param>
        /// <param name="token">The cancellation token</param>
        /// <returns>A valid response</returns>
        public async Task<HttpResponseMessage> DownloadContent(string url, CancellationToken token)
        {
            return await this.ExecuteWithRetries(
                 async () =>
                 {
                     return await this.webSystemCalls.DownloadContent(url, token).ConfigureAwait(continueOnCapturedContext: false);
                 })
                 .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Call DownloadPartialContent with retries
        /// </summary>
        /// <param name="url">The url</param>
        /// <param name="from">From value, in bytes</param>
        /// <param name="to">From value, in bytes</param>
        /// <param name="token">The cancellation tokem</param>
        /// <returns>A valid response</returns>
        public async Task<HttpResponseMessage> DownloadPartialContent(string url, long from, long to, CancellationToken token)
        {
            return await this.ExecuteWithRetries(
            async () =>
            {
                return await this.webSystemCalls.DownloadPartialContent(url, from, to, token).ConfigureAwait(continueOnCapturedContext: false);
            })
            .ConfigureAwait(false);
        }

        /// <summary>
        /// Call GetHeadersAsync with retries
        /// </summary>
        /// <param name="url">The url</param>
        /// <param name="token">The cancellation token</param>
        /// <returns>A valid response</returns>
        public async Task<HttpResponseMessage> GetHeadersAsync(string url, CancellationToken token)
        {
            return await this.ExecuteWithRetries(
            async () =>
            {
                 return await this.webSystemCalls.GetHeadersAsync(url, token).ConfigureAwait(continueOnCapturedContext: false);
            })
            .ConfigureAwait(false);
        }

        /// <summary>
        /// Execute the tasks with retries
        /// </summary>
        /// <param name="task">The given task</param>
        /// <returns>A valid response</returns>
        private async Task<HttpResponseMessage> ExecuteWithRetries(Func<Task<HttpResponseMessage>> task)
        {
            TimeSpan waitTime = TimeSpan.Zero;
            int retryCount = 0;

            while (true)
            {
                if (waitTime > TimeSpan.Zero)
                {
                    await this.Delay(waitTime).ConfigureAwait(false);
                }

                try
                {
                    return await task().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    waitTime = (retryCount <= 2) ? this.DefaultWait : this.DefaultWaitAfterTwoRetries ;
                    Console.WriteLine($"Connection Failure - Will retry after {waitTime} seconds");
                }

                retryCount++;
            }
        }
    }
}
