using System;
using System.Linq;
using System.Text.RegularExpressions;
using OpenQA.Selenium;

namespace Linker2.Extensions;

public static class WebElementExtensions
{
    private static readonly Regex classNameValidatorRegex = new(@"^[a-z][a-z0-9\-_]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex whiteSpaceRegex = new(@"\s+");

    public static bool HasClass(this IWebElement element, params string[] htmlClasses)
    {
        if (htmlClasses.Length == 0)
        {
            throw new ArgumentException("No html classes to match.");
        }

        if (!htmlClasses.All(classNameValidatorRegex.IsMatch))
        {
            throw new ArgumentException("Invalid CSS class(es) detected.");
        }

        var classAttribute = element.GetAttribute("class");
        if (string.IsNullOrWhiteSpace(classAttribute))
        {
            return false;
        }

        var elementClasses = whiteSpaceRegex.Split(classAttribute.Trim()).ToHashSet();

        return htmlClasses.All(elementClasses.Contains);
    }

    public static bool HasAnyClass(this IWebElement element, params string[] htmlClasses)
    {
        if (htmlClasses.Length == 0)
        {
            throw new ArgumentException("No html classes to match.");
        }

        if (!htmlClasses.All(classNameValidatorRegex.IsMatch))
        {
            throw new ArgumentException("Invalid CSS class(es) detected.");
        }

        try
        {
            var classAttribute = element.GetAttribute("class");
            if (string.IsNullOrWhiteSpace(classAttribute))
            {
                return false;
            }

            var elementClasses = whiteSpaceRegex.Split(classAttribute.Trim()).ToHashSet();

            return htmlClasses.Any(elementClasses.Contains);
        }
        catch (StaleElementReferenceException e)
        {
            return false;
        }
    }
}
