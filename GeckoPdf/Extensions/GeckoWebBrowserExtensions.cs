using Gecko;
using GeckoPdf.Config;

namespace GeckoPdf.Extensions
{
    public static class GeckoWebBrowserExtensions
    {
        public static void PrintToFile(this GeckoWebBrowser browser, GeckoPdfConfig config, string filePath)
        {
            if (config.PageMargins == null)
                config.PageMargins = new GeckoMargins(0, 0, 0, 0);

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

            ps.SetFooterStrCenterAttribute(config.FooterCenter);
            ps.SetFooterStrLeftAttribute(config.FooterLeft);
            ps.SetFooterStrRightAttribute(config.FooterRight);

            ps.SetHeaderStrCenterAttribute("");
            ps.SetHeaderStrRightAttribute("");
            ps.SetHeaderStrLeftAttribute("");

            ps.SetMarginTopAttribute(config.PageMargins.Top);
            ps.SetMarginBottomAttribute(config.PageMargins.Bottom);
            ps.SetMarginRightAttribute(config.PageMargins.Right);
            ps.SetMarginLeftAttribute(config.PageMargins.Left);

            print.Print(ps, null);
        }
    }
}
