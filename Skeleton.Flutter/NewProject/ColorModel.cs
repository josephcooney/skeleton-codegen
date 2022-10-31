using System.Drawing;

namespace Skeleton.Flutter.NewProject
{
    public class ColorModel
    {
        public ColorModel(string hexRgbValue)
        {
            var color = System.Drawing.ColorTranslator.FromHtml(hexRgbValue);
            Red = color.R;
            Green = color.G;
            Blue = color.B;
        }

        public int Red { get; }

        public int Green { get; }

        public int Blue { get; }

        public string ARGBHex => $"0xFF{Red.ToString("x2")}{Green.ToString("x2")}{Blue.ToString("x2")}";
    }
}
