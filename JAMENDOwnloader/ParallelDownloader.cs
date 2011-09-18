using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Net;

namespace JAMENDOwnloader
{
    public class ParallelDownloader
    {
        private byte MaximumNumberOfConcurrentDownloads;
        private byte CurrentConcurrentDownloads;

        public ParallelDownloader(byte NumberOfConcurrentDownloads)
        {
            MaximumNumberOfConcurrentDownloads = NumberOfConcurrentDownloads;
        }
     
        public void AddToQueue(String URL, String Filename)
        {
            while (CurrentConcurrentDownloads >= MaximumNumberOfConcurrentDownloads)
            {
                Thread.Sleep(100);
            }

            CurrentConcurrentDownloads++;
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadFileAsync(new Uri(URL), Filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: "+URL+" -> "+Filename);
                Console.WriteLine(e.Message);
            }
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            CurrentConcurrentDownloads--;
        }
    }
}
