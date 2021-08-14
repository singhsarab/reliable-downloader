﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReliableWebSystemCallsTests.cs">
//   Copyright 2021 Sarabjot Singh. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ReliableDownloader.Tests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class ReliableWebSystemCallsTests
    {
        Mock<IWebSystemCalls> mockWebSystemCalls;
        ReliableWebSystemCalls reliableWebSystemCalls;

        [SetUp]
        public void Setup()
        {
            mockWebSystemCalls = new Mock<IWebSystemCalls>();
            reliableWebSystemCalls = new ReliableWebSystemCalls(mockWebSystemCalls.Object);
        }

        [Test]
        public async Task DownloadContentShouldGetCalledOnceIfResponseIsValid()
        {
            mockWebSystemCalls.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>())).
                ReturnsAsync(
                () => new HttpResponseMessage());

            var token = new CancellationTokenSource().Token;
            await reliableWebSystemCalls.DownloadContent("", token);

            mockWebSystemCalls.Verify(x => x.DownloadContent("", token), Times.Once);
        }

        [Test]
        public void DownloadContentShouldRetryAfter2SecondsForFirstTwoTimes()
        {
            int times = 0;
            mockWebSystemCalls.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>())).
                ReturnsAsync(
                () =>
                {
                    times++;
                    if (times > 2)
                        return new HttpResponseMessage();
                    else
                        throw new Exception();
                });

            var tokenSource = new CancellationTokenSource();
            Task task = reliableWebSystemCalls.DownloadContent("", tokenSource.Token);

            Thread.Sleep(4000);

            mockWebSystemCalls.Verify(x => x.DownloadContent("", tokenSource.Token), Times.Exactly(3));
        }
    }
}
