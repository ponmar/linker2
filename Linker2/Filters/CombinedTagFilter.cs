using Linker2.Model;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class CombinedTagFilter : ILinkFilter
{
    public required IEnumerable<string> Tags { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        if (Tags.Any())
        {
            return links.Where(l => Tags.All(x => l.Tags.Any(y => y == x)));
        }
        return links;
    }
}
