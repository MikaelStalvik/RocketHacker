using System.Windows.Media;

namespace RocketModder
{
    public static class Utils
    {
        public static Color ParseColor(this string colorCode)
        {
            if (!colorCode.StartsWith('#')) colorCode = "#" + colorCode;
            return (Color)ColorConverter.ConvertFromString(colorCode);
        }
        public static bool IsOdd(this int value)
        {
            return value % 2 != 0;
        }
    }
}
