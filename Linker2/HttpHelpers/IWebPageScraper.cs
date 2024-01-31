using System.Collections.Generic;

namespace Linker2.HttpHelpers;

public interface IWebPageScraper
{
    public bool Load(string url);
    public void Close();

    public string? PageTitle { get; }
    public List<string> GetImageSrcs(List<string> preferredImageIds);
}
