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

        public int MaxLockingCheckAttempts { get; set; } = 100;

        public int PdfLockingCheckDelay { get; set; } = 100;

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
                    Thread.Sleep(1000); // Some delay need, otherwise throws COMException
                    printReadySignal.Release();
                });
            };

            // Invoke navigate method in STA browser thread
            browser.Invoke((MethodInvoker)delegate
            {
                browser.Navigate(url);
            });

            printReadySignal.Wait(); // Waiting before print will completed

            // Create temporary file and print page to him
            var filePath = Path.GetTempFileName();
            browser.Invoke((MethodInvoker)delegate
            {
                browser.PrintToFile(_config, filePath);
            });
            
            var attempts = 0;

            while(Tools.IsFileLocked(filePath) || attempts >= MaxLockingCheckAttempts)
            {
                attempts++;
                Thread.Sleep(PdfLockingCheckDelay);
            }

            if (attempts >= MaxLockingCheckAttempts)
            {
                throw new IOException("Generated pdf file locked too long");
            }

            // Read temporary file to MemoryStream and delete
            var ms = new MemoryStream(File.ReadAllBytes(filePath));
            File.Delete(filePath);

            browser.Invoke((MethodInvoker)delegate
            {
                browser.Dispose();
            });

            return ms;
        }

        public async Task<MemoryStream> ConvertAsync(string url)
        {
            var ms = await Task.Run(() => Convert(url));
            return ms;
        }

        /// <summary>
        /// Unloads gecko engine. Need to be invoked always, when application shutdown
        /// </summary>
        public static void UnloadGecko()
        {
            HeadlessGecko.Unload();
        }
    }
}
