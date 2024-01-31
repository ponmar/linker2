using Linker2.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class HiddenTagsFilter : ILinkFilter
{
    public required IEnumerable<string> HiddenTags { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        return links.Where(x => !x.Tags.Any(HiddenTags.Contains));
    }
}
