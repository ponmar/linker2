using Linker2.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class TextFilter : ILinkFilter
{
    public required string Text { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            return links.Where(x =>
                (x.Title is not null && x.Title.Contains(Text, StringComparison.OrdinalIgnoreCase)) ||
                x.Tags.Any(t => t.Contains(Text, StringComparison.OrdinalIgnoreCase)) ||
                x.Url.Contains(Text, StringComparison.OrdinalIgnoreCase));
        }
        return links;
    }
}
