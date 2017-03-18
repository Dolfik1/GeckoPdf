using Gecko;
using Gecko.IO;
using GeckoPdf.Config;
using GeckoPdf.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeckoPdf
{
    public class GeckoPdf
    {
        public static string GeckoBinDirectory { get; set; } = "Firefox";

        private GeckoPdfConfig _config;

        /// <summary>
        /// A HTML to PDF converter based on Gecko engine
        /// </summary>
        /// <param name="config">Convert config</param>
        public GeckoPdf(GeckoPdfConfig config)
        {
            _config = config;
        }

        private byte[] ConvertToPdf(string url, string html, string tempFilePath, NameValueCollection headers = null, IEnumerable<GeckoCookie> cookies = null)
        {

            // Initialize gecko engine
            if (!HeadlessGecko.IsInitialized)
            {
                var initSignal = new SemaphoreSlim(0, 1);
                HeadlessGecko.OnInitialized += () =>
                {
                    initSignal.Release();
                };

                HeadlessGecko.Initialize(GeckoBinDirectory);
                initSignal.Wait();
            }

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
                Uri uri = null;
                var isUriSuccess = Uri.TryCreate(url, UriKind.Absolute, out uri);

                if (isUriSuccess && cookies != null)
                {
                    foreach (var cookie in cookies)
                    {
                        CookieManager.Add(uri.Host, cookie.Path, cookie.Name, cookie.Value, cookie.Secure, cookie.HttpOnly, false, cookie.ExpiresUnix);
                    }
                }
                var geckoHeaders = MimeInputStream.Create();
                if (headers != null)
                {
                    var headersItems = headers.AllKeys.SelectMany(headers.GetValues, (k, v) => new { Key = k, Value = v });
                    foreach (var header in headersItems)
                    {
                        geckoHeaders.AddHeader(header.Key, header.Value);
                    }
                    return;
                }
                else
                {
                    geckoHeaders.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0");
                    geckoHeaders.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    geckoHeaders.AddHeader("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                    geckoHeaders.AddHeader("Accept-Encoding", "gzip, deflate, br");
                    geckoHeaders.AddHeader("X-Compress", "1");
                    geckoHeaders.AddHeader("Connection", "keep-alive");
                }

                if (string.IsNullOrEmpty(html))
                    browser.Navigate(url, GeckoLoadFlags.None, url, null, geckoHeaders);
                else
                    browser.LoadHtml(html, url);

            });

            printReadySignal.Wait(); // Waiting before print will completed
            //browser.NavigateFinishedNotifier.BlockUntilNavigationFinished();

            // Create temporary file and print page to him
            if (string.IsNullOrEmpty(tempFilePath))
                tempFilePath = Path.GetTempFileName();

            var fi = new FileInfo(tempFilePath);
            if (!fi.Exists)
            {
                if (!fi.Directory.Exists)
                    fi.Directory.Create();
                File.Create(tempFilePath).Close();
            }

            browser.Invoke((MethodInvoker)delegate
            {
                browser.PrintToFile(_config, tempFilePath);
            });

            var attempts = 0;

            while (Tools.IsFileLocked(tempFilePath) || attempts >= _config.MaxLockingCheckAttempts)
            {
                attempts++;
                Thread.Sleep(_config.PdfLockingCheckDelay);
            }

            if (attempts >= _config.MaxLockingCheckAttempts)
            {
                throw new IOException("Generated pdf file locked too long");
            }

            // Read temporary file to MemoryStream and delete
            var bytes = File.ReadAllBytes(tempFilePath);
            File.Delete(tempFilePath);

            browser.Invoke((MethodInvoker)delegate
            {
                browser.Dispose();
            });

            return bytes;
        }

        /// <summary>
        /// Converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <param name="tempFilePath">Location of temporary file (file will be deleted after using)</param>
        /// <returns></returns>
        public byte[] Convert(string url, string tempFilePath = "")
        {
            return ConvertToPdf(url, tempFilePath, string.Empty);
        }

        /// <summary>
        /// Converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <param name="headers">Browser headers</param>
        /// <param name="cookies">Browser cookies</param>
        /// <param name="tempFilePath">Location of temporary file (file will be deleted after using)</param>
        /// <returns></returns>
        public byte[] Convert(string url, NameValueCollection headers, IEnumerable<GeckoCookie> cookies, string tempFilePath = "")
        {
            return ConvertToPdf(url, string.Empty, tempFilePath, headers, cookies);
        }

        /// <summary>
        /// Asynchronously converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <returns></returns>
        public async Task<byte[]> ConvertAsync(string url, string tempFilePath = "")
        {
            return await Task.Run(() => Convert(url));
        }

        /// <summary>
        /// Asynchronously converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <param name="headers">Browser headers</param>
        /// <param name="cookies">Browser cookies</param>
        /// <param name="tempFilePath">Location of temporary file (file will be deleted after using)</param>
        /// <returns></returns>
        public async Task<byte[]> ConvertAsync(string url, NameValueCollection headers, IEnumerable<GeckoCookie> cookies, string tempFilePath = "")
        {
            return await Task.Run(() => Convert(url, headers, cookies));
        }

        public byte[] ConvertHtml(string url, string html, NameValueCollection headers, IEnumerable<GeckoCookie> cookies, string tempFilePath = "")
        {
            return ConvertToPdf(url, html, tempFilePath, headers, cookies);
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
