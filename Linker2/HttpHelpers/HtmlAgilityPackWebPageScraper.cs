﻿using HtmlAgilityPack;
using Linker2.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.HttpHelpers;

public class HtmlAgilityPackWebPageScraper : IWebPageScraper
{
    private readonly HtmlWeb htmlWeb = new();
    private HtmlDocument? htmlDoc;

    public HtmlAgilityPackWebPageScraper()
    {
        htmlWeb.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:101.0) Gecko/20100101 Firefox/101.0";
    }

    public void Close()
    {
        htmlDoc = null;
    }

    public bool Load(string url)
    {
        try
        {
            htmlDoc = htmlWeb.LoadFromWebAsync(url).Result;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string? PageTitle => htmlDoc?.DocumentNode.SelectSingleNode("//head/title").InnerHtml;

    public List<string> GetImageSrcs(List<string> preferredImageIds)
    {
        var result = new List<string>();
        if (htmlDoc is null)
        {
            return result;
        }

        foreach (var imageId in preferredImageIds)
        {
            var element = htmlDoc!.GetElementbyId(imageId);
            var imageSrc = element?.GetAttributeValue("src", string.Empty);
            if (imageSrc.HasContent())
            {
                result.Add(imageSrc!);
            }
        }

        var allImageSrcs = GetAllImageSrcs();
        result.AddRange(allImageSrcs);

        return result.Distinct().ToList();
    }

    private List<string> GetAllImageSrcs()
    {
        if (htmlDoc is null)
        {
            return [];
        }
        return htmlDoc.DocumentNode.Descendants("img")
            .Select(e => e.GetAttributeValue("src", string.Empty))
            .Where(s => s.HasContent() && (s.StartsWith("https://") || s.StartsWith("http://")) && !s.Contains("svg")).ToList();
    }
}
