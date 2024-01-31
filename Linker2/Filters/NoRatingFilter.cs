using Linker2.Model;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class NoRatingFilter : ILinkFilter
{
    public bool Enabled { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        return Enabled ? links.Where(x => x.Rating is null) : links;
    }
}
