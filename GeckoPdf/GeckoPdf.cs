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
        
        private MemoryStream ConvertToPdf(string url, string html, NameValueCollection headers = null, IEnumerable<GeckoCookie> cookies = null)
        {

            // Initialize gecko engine
            if (!HeadlessGecko.IsInitialized)
                HeadlessGecko.InitializeAsync(GeckoBinDirectory).Wait();

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

            Uri uri = null;
            var isUriSuccess = Uri.TryCreate(url, UriKind.Absolute, out uri);

            if (isUriSuccess && cookies != null)
            {
                foreach (var cookie in cookies)
                {
                    CookieManager.Add(uri.Host, cookie.Path, cookie.Name, cookie.Value, cookie.Secure, cookie.HttpOnly, false, cookie.ExpiresUnix);
                }
            }

            // Invoke navigate method in STA browser thread
            browser.Invoke((MethodInvoker)delegate
            {
                MimeInputStream geckoHeaders = null;
                if (headers != null)
                {
                    geckoHeaders = MimeInputStream.Create();
                    var headersItems = headers.AllKeys.SelectMany(headers.GetValues, (k, v) => new { Key = k, Value = v });
                    foreach (var header in headersItems)
                    {
                        geckoHeaders.AddHeader(header.Key, header.Value);
                    }
                    return;
                }

                if (geckoHeaders == null)
                {
                    if (string.IsNullOrEmpty(html))
                        browser.Navigate(url, GeckoLoadFlags.None);
                    else
                        browser.LoadHtml(html, url);
                }
                else
                {
                    if (string.IsNullOrEmpty(html))
                        browser.Navigate(url, GeckoLoadFlags.None, "", null, geckoHeaders);
                    else
                        browser.LoadHtml(html, url);
                }
            });

            printReadySignal.Wait(); // Waiting before print will completed

            // Create temporary file and print page to him
            var filePath = Path.GetTempFileName();
            browser.Invoke((MethodInvoker)delegate
            {
                browser.PrintToFile(_config, filePath);
            });

            var attempts = 0;

            while (Tools.IsFileLocked(filePath) || attempts >= _config.MaxLockingCheckAttempts)
            {
                attempts++;
                Thread.Sleep(_config.PdfLockingCheckDelay);
            }

            if (attempts >= _config.MaxLockingCheckAttempts)
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

        /// <summary>
        /// Converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <returns></returns>
        public MemoryStream Convert(string url)
        {
            return ConvertToPdf(url, string.Empty) ;
        }
        
        /// <summary>
        /// Converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <param name="headers">Browser headers</param>
        /// <param name="cookies">Browser cookies</param>
        /// <returns></returns>
        public MemoryStream Convert(string url, NameValueCollection headers, IEnumerable<GeckoCookie> cookies)
        {
            return ConvertToPdf(url, string.Empty, headers, cookies);
        }

        /// <summary>
        /// Asynchronously converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <returns></returns>
        public async Task<MemoryStream> ConvertAsync(string url)
        {
            var ms = await Task.Run(() => Convert(url));
            return ms;
        }

        /// <summary>
        /// Asynchronously converts HTML page to PDF from specified url
        /// </summary>
        /// <param name="url">URL address to convert</param>
        /// <param name="headers">Browser headers</param>
        /// <param name="cookies">Browser cookies</param>
        /// <returns></returns>
        public async Task<MemoryStream> ConvertAsync(string url, NameValueCollection headers, IEnumerable<GeckoCookie> cookies)
        {
            var ms = await Task.Run(() => Convert(url, headers, cookies));
            return ms;
        }

        public MemoryStream ConvertHtml(string url, string html, NameValueCollection headers, IEnumerable<GeckoCookie> cookies)
        {
            return ConvertToPdf(url, html, headers, cookies);
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
