using Linker2.Model;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class SiteFilter : ILinkFilter
{
    public required string? Site { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        if (Site is not null)
        {
            return links.Where(x => x.Url.Contains(Site));
        }
        return links;
    }
}
