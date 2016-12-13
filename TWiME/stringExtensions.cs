using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Extensions
{
    public static class stringExtensions
    {
        public static int Width(this string str, Font withFont)
        {
            int width = TextRenderer.MeasureText(str, withFont).Width;
            return width;
        }

        public static int Height(this string str, Font withFont)
        {
            int height = TextRenderer.MeasureText(str, withFont).Height;
            return height;
        }

        public static string With(this string str, params object[] formatWith)
        {
            return String.Format(str, formatWith);
        }

        public static bool Glob(this string str, string match)
        {
            match = Regex.Escape(match).Replace(@"\*", ".*").Replace(@"\?", ".");
            return Regex.IsMatch(str, match, RegexOptions.IgnoreCase);
        }

        public static Color ToColor(this string color)
        {
            if (color.StartsWith("#"))
            {
                return Color.FromArgb(
                    int.Parse(color.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier),
                    int.Parse(color.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier),
                    int.Parse(color.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            }

            if (color.Contains(","))
            {
                var rgb = color.Split(',');
                if (rgb.Length > 3)
                {
                    return Color.FromArgb(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
                }
                else
                {
                    return Color.FromArgb(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]), int.Parse(rgb[3]));
                }
            }

            return Color.FromName(color);
        }
    }
}