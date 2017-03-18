using Gecko;
using GeckoPdf.Config;

namespace GeckoPdf.Extensions
{
    public static class GeckoWebBrowserExtensions
    {
        public static void PrintToFile(this GeckoWebBrowser browser, GeckoPdfConfig config, string filePath)
        {
            var print = Xpcom.QueryInterface<nsIWebBrowserPrint>(browser.Window.DomWindow);
            var ps = print.GetGlobalPrintSettingsAttribute();

            
            ps.SetPrintSilentAttribute(true);
            ps.SetPrintToFileAttribute(true);
            ps.SetShowPrintProgressAttribute(false);
            ps.SetOutputFormatAttribute(2); //2 == PDF

            ps.SetToFileNameAttribute(filePath);

            ps.SetPrintBGImagesAttribute(false);
            ps.SetStartPageRangeAttribute(config.StartPageRange);
            ps.SetEndPageRangeAttribute(config.EndPageRange);
            ps.SetPrintOptions(2, config.PrintEvenPages); // evenPages
            ps.SetPrintOptions(1, config.PrintOddPages); // oddpages
            //ps.SetEffectivePageSize(768 * 20f, 1024 * 20f);
            ps.SetShrinkToFitAttribute(config.ShrinkToFit);
            ps.SetScalingAttribute(config.DocumentScale);

            ps.SetFooterStrCenterAttribute("");
            ps.SetFooterStrLeftAttribute("");
            ps.SetFooterStrRightAttribute("");

            ps.SetHeaderStrCenterAttribute("");
            ps.SetHeaderStrRightAttribute("");
            ps.SetHeaderStrLeftAttribute("");
            
            

            print.Print(ps, null);
        }
    }
}
