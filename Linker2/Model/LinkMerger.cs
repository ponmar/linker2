using System.Linq;
using System;

namespace Linker2.Model;

public class LinkMerger
{
    public static LinkDto MergeLinks(LinkDto first, LinkDto second)
    {
        if (first.Url != second.Url)
        {
            throw new ArgumentException("Merging links only supported for same URL");
        }

        var title = string.IsNullOrEmpty(first.Title) ? second.Title : first.Title;
        var thumbnailUrl = string.IsNullOrEmpty(first.ThumbnailUrl) ? second.ThumbnailUrl : first.ThumbnailUrl;
        var rating = first.Rating is null ? second.Rating : first.Rating;
        var openCounter = Math.Max(first.OpenCounter, second.OpenCounter);
        var dateTime = first.DateTime > second.DateTime ? first.DateTime : second.DateTime;

        var tags = first.Tags.ToList();
        tags.AddRange(second.Tags.Where(x => !tags.Contains(x)));
        tags.Sort();

        return new LinkDto(title, tags, first.Url, dateTime, rating, thumbnailUrl, openCounter);
    }
}
