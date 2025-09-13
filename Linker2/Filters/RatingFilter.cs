using Linker2.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class RatingFilter : ILinkFilter
{
    public required int? Rating { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        return Rating is not null ? links.Where(x => x.Rating == Rating) : links;
    }
}
