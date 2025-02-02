﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs">
//   Copyright 2021 Sarabjot Singh. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ReliableDownloader
{
    using System;
    using System.Threading.Tasks;

    internal class Program
    {
        public static void Main(string[] args)
        {
            FileDownloader fileDownloader = new FileDownloader();

            string exampleUrl = @"https://installerstaging.accurx.com/chain/3.55.11050.0/accuRx.Installer.Local.msi";
            string exampleFilePath = "C:/Users/sasin/accuRx.Installer.Local.msi";

            // TODO: Write a better progress indicator
            Task<bool> downloadTask = fileDownloader.DownloadFile(exampleUrl, exampleFilePath,
                progress => { Console.WriteLine($"MyFile1.msi File Progress :{progress.ProgressPercent}% and Remaining Time in secs : {progress.EstimatedRemaining.Value.TotalSeconds}"); });

            /* Run another download in parallel
            
            exampleFilePath = "C:/Users/sasin/MyFile2.msi";
            Task t2 = fileDownloader.DownloadFile(exampleUrl, exampleFilePath,
                progress => { Console.WriteLine($"MyFile2.msi File Progress :{progress.ProgressPercent}% and Remaining Time in secs : {progress.EstimatedRemaining.Value.TotalSeconds}"); });
            */

            downloadTask.Start();
            downloadTask.Wait();

            if (downloadTask.Result)
                Console.WriteLine("File downloaded successfully");
            else
                Console.WriteLine("File download had some error.");

            /* Testing Cancel
             * Remove the wait above and sleep for 4 seconds and then call cancel
            
            Thread.Sleep(4000);
            Console.WriteLine("Cancelling downloads...");
            fileDownloader.CancelDownloads();
            
             */
        }
    }
}