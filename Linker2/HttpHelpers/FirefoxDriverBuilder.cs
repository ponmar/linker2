using System;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium.Firefox;

namespace Linker2.HttpHelpers;

public class FirefoxDriverBuilder
{
    public string DriverPath { get; private set; } = ".";
    public bool Headless { get; private set; } = true;

    public FirefoxDriverBuilder WithDriverPath(string path)
    {
        DriverPath = path;
        return this;
    }

    public FirefoxDriverBuilder WithHeadless(bool headless)
    {
        Headless = headless;
        return this;
    }

    public FirefoxDriver Build()
    {
        return FirefoxDriverFactory.CreateFirefoxDriver(DriverPath, Headless);
    }
}
