using Microsoft.VisualStudio.TestTools.UnitTesting;
using GeckoPdf.Config;
using System.Threading.Tasks;
using System.Net.Http;

namespace GeckoPdf.Tests
{
    [TestClass]
    public class PrintTest
    {
        private const int EmptyDocLength = 829;

        [TestMethod]
        public void PrintGoogle()
        {
            var pdf = new GeckoPdf(new GeckoPdfConfig());
            var bytes = pdf.Convert("https://google.com");
            Assert.IsTrue(bytes.Length > EmptyDocLength);

            GeckoPdf.UnloadGecko();
        }

        [TestMethod]
        public void PrintW3Html()
        {
            using (var client = new HttpClient())
            {
                var url = "https://www.w3.org";
                var r = client.GetStringAsync(url);

                r.Wait();

                var pdf = new GeckoPdf(new GeckoPdfConfig());
                var bytes = pdf.ConvertHtml(url, r.Result, null, null);
                Assert.IsTrue(bytes.Length > EmptyDocLength);

                GeckoPdf.UnloadGecko();
            }
        }

        [TestMethod]
        public void PrintMultipleSitesAtOnce()
        {
            var sites = new string[]
            {
                "https://google.com",
                "https://www.w3.org",
                "https://www.microsoft.com",
                "https://github.com/",
                "https://www.mozilla.org"
            };
            
            foreach (var site in sites)
            {
                Task.Run(() =>
                {
                    var pdf = new GeckoPdf(new GeckoPdfConfig());
                    var bytes = pdf.Convert(site);
                    Assert.IsTrue(bytes.Length > EmptyDocLength);
                });
            }
        }
    }
}
