using OpenQA.Selenium.Firefox;

namespace Linker2.HttpHelpers;

class FirefoxDriverFactory
{
    public static FirefoxDriver CreateFirefoxDriver(string driverPath, bool headless)
    {
        var options = new FirefoxOptions();
        if (headless)
        {
            options.AddArgument("-headless");
        }
        return new FirefoxDriver(driverPath, options);
    }
}
