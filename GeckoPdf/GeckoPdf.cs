using Gecko;
using GeckoPdf.Config;
using GeckoPdf.Extensions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeckoPdf
{
    public class GeckoPdf
    {
        private GeckoPdfConfig _config;

        /// <summary>
        /// A HTML to PDF converter based on Gecko engine
        /// </summary>
        /// <param name="config">Convert config</param>
        public GeckoPdf(GeckoPdfConfig config)
        {
            _config = config;
        }

        public MemoryStream Convert(string url)
        {
            // Initialize gecko engine
            if (!HeadlessGecko.IsInitialized)
                HeadlessGecko.InitializeAsync().Wait();


            var printReadySignal = new SemaphoreSlim(0, 1);

            var browser = HeadlessGecko.CreateBrowser();
            browser.DocumentCompleted += (s, e) =>
            {
                // After loading document, waits some time, and send print ready signal
                Task.Run(() =>
                {
                    Thread.Sleep(50); // Some delay need, otherwise throws COMException
                    printReadySignal.Release();
                });
            };

            // Invoke navigate method in STA browser thread
            browser.TopLevelControl.Invoke((MethodInvoker)delegate
            {
                browser.Navigate(url);
            });

            printReadySignal.Wait(); // Wait print available
            printReadySignal = new SemaphoreSlim(0, 1);

            // Create temporary file and print page to him
            var filePath = Path.GetTempFileName();
            browser.TopLevelControl.Invoke((MethodInvoker)delegate
            {
                browser.PrintToFile(_config, filePath);
                printReadySignal.Release();
            });

            printReadySignal.Wait(); // Waiting before print will completed

            // Read temporary file to MemoryStream and delete
            var ms = new MemoryStream(File.ReadAllBytes(filePath));
            File.Delete(filePath);

            browser.Dispose();

            return ms;
        }

        public async Task<MemoryStream> ConvertAsync(string url)
        {
            var ms = await Task.Run(() => Convert(url));
            return ms;
        }
    }
}
