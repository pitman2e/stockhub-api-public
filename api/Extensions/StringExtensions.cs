namespace StockHub.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    public static string IsNullOrWhiteSpaceThen(this string str1, string str2)
    {
        if (!string.IsNullOrWhiteSpace(str1))
        {
            return str1;
        }
        else
        {
            return str2;
        }
    }
}