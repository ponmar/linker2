using System.Collections.Generic;
using System.Linq;
using Linker2.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace Linker2.HttpHelpers;

public class FirefoxWebPageScraper : IWebPageScraper
{
    private readonly FirefoxDriver driver;

    public FirefoxWebPageScraper(string driverPath, bool headless)
    {
        driver = new FirefoxDriverBuilder().WithDriverPath(driverPath).WithHeadless(headless).Build();
    }

    public void Close()
    {
        driver.Close();
        driver.Quit();
    }

    public bool Load(string url)
    {
        try
        {
            if (url != driver.Url)
            {
                driver.Url = url;
                driver.Navigate();
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string? PageTitle
    {
        get
        {
            try
            {
                var title = driver.Title;
                if (title.Contains(" - ") && title.EndsWith(".com"))
                {
                    return title[..title.LastIndexOf(" - ")];
                }
                return title;
            }
            catch
            {
                return null;
            }
        }
    }

    public List<string> GetImageSrcs(List<string> preferredImageIds)
    {
        var results = new List<string>();

        // Images inside divs with preferred classes
        var divElements = driver.FindElements(By.TagName("div"));
        foreach (var divElement in divElements)
        {
            if (divElement.HasAnyClass(preferredImageIds.ToArray()))
            {
                divElement.FindElements(By.TagName("img")).ToList().ForEach(imgTag =>
                {
                    var imageSrc = imgTag.GetDomAttribute("src");
                    if (IsValidImageSrc(imageSrc) && !results.Contains(imageSrc!))
                    {
                        results.Add(imageSrc!);
                    }
                });
            }
        }

        // Find images by IDs
        foreach (var imageId in preferredImageIds)
        {
            try
            {
                var imgTag = driver.FindElement(By.Id(imageId));
                var imageSrc = imgTag.GetDomAttribute("src");
                if (IsValidImageSrc(imageSrc) && !results.Contains(imageSrc!))
                {
                    results.Add(imageSrc!);
                }
            }
            catch (InvalidSelectorException)
            {
            }
            catch (NoSuchElementException)
            {
            }
        }

        // All other images
        var imgElements = driver.FindElements(By.TagName("img"));
        foreach (var imgElement in imgElements)
        {
            var imageSrc = imgElement.GetDomAttribute("src");
            if (IsValidImageSrc(imageSrc) && !results.Contains(imageSrc!))
            {
                results.Add(imageSrc!);
            }
        }

        return results;
    }

    private static bool IsValidImageSrc(string? imageSrc)
    {
        return imageSrc is not null &&
               (imageSrc.StartsWith("http://") || imageSrc.StartsWith("https://")) &&
               !imageSrc.Contains("svg");
    }
}
