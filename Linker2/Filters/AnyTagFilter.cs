using Linker2.Model;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class AnyTagFilter : ILinkFilter
{
    public required IEnumerable<string> Tags { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        if (Tags.Any())
        {
            return links.Where(x => x.Tags.Any(t => Tags.Contains(t)));
        }
        return links;
    }
}
