// --------------------------------------------------------------------------------------------------------------------
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
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class FileDownloaderTests
    {
        private string filePathUrl = "https://foobar.com/images/hello.jpg";
        private string localfilePath = @"C:\temp\hello.jpg";
       
        Mock<IWebSystemCalls> mockWebSystemCalls;
        FileDownloader fileDownloader;
        Action<FileProgress> onProgressChanged;
        TestableHttpContent testableHttpContent;

        [SetUp]
        public void Setup()
        {
            mockWebSystemCalls = new Mock<IWebSystemCalls>();
            fileDownloader = new FileDownloader(mockWebSystemCalls.Object);

            testableHttpContent = new TestableHttpContent();
            onProgressChanged = (x => Console.WriteLine($"Percent progress is {x.ProgressPercent}"));
        }

        [Test]
        public void DownContentShouldDownloadContent()
        {
            mockWebSystemCalls.Setup(x => x.GetHeadersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).
              ReturnsAsync(
              () =>
                  {
                      var response = new HttpResponseMessage();
                      response.StatusCode = System.Net.HttpStatusCode.OK;
                      response.Content = testableHttpContent;
                      return response;
                  }
              );

            mockWebSystemCalls.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>())).
               ReturnsAsync(
               () => new HttpResponseMessage());

            var token = new CancellationTokenSource().Token;

            Task task = fileDownloader.DownloadFile(filePathUrl, localfilePath, onProgressChanged);

            task.Start();
            task.Wait();

            mockWebSystemCalls.Verify(x => x.GetHeadersAsync("https://foobar.com/images/hello.jpg", token), Times.Once);
            mockWebSystemCalls.Verify(x => x.DownloadContent(filePathUrl, token), Times.Once);
        }


    }
}
