// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReliableWebSystemCallsTests.cs">
//   Copyright 2021 Sarabjot Singh. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReliableDownloader.Tests
{
    internal class TestableHttpContent : HttpContent
    {
        public TestableHttpContent()
        {
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.Run(() => true);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 10;
            return true;
        }
    }
}