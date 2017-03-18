namespace GeckoPdf.Config
{
    public class GeckoMargins
    {
        public double Top { get; set; } = 0;

        public double Right { get; set; } = 0;

        public double Bottom { get; set; } = 0;

        public double Left { get; set; } = 0;

        public GeckoMargins(double top, double right, double bottom, double left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}
