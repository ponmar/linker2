using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.HttpHelpers;

public class FirefoxWebPageScraper : IWebPageScraper
{
    private readonly FirefoxDriver driver;

    public FirefoxWebPageScraper(string driverPath, bool headless)
    {
        var options = new FirefoxOptions();
        if (headless)
        {
            options.AddArgument("-headless");
        }
        driver = new FirefoxDriver(driverPath, options);
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
                    return title.Substring(0, title.LastIndexOf(" - "));
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
        var result = new List<string>();
        foreach (var imageId in preferredImageIds)
        {
            var imageSrc = GetImageSrc(imageId);
            if (imageSrc is not null)
            {
                result.Add(imageSrc);
            }
        }

        var allImageSrcs = GetAllImageSrcs();
        result.AddRange(allImageSrcs);

        return result.Distinct().ToList();
    }

    private string? GetImageSrc(string imageId)
    {
        try
        {
            var imgTag = driver.FindElement(By.Id(imageId));
            return imgTag.GetDomAttribute("src");
        }
        catch (InvalidSelectorException)
        {
        }
        catch (NoSuchElementException)
        {
        }
        return null;
    }

    private List<string> GetAllImageSrcs()
    {
        var result = new List<string>();
        foreach (var imageTag in driver.FindElements(By.TagName("img")))
        {
            try
            {
                var imageSrc = imageTag.GetDomAttribute("src");
                if (imageSrc is not null &&
                    (imageSrc.StartsWith("http://") || imageSrc.StartsWith("https://")) &&
                    !imageSrc.Contains("svg"))
                {
                    result.Add(imageSrc);
                }
            }
            catch
            {
            }
        }
        return result;
    }
}
