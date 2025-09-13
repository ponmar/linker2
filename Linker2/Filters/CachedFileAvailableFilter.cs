using Linker2.Configuration;
using Linker2.Model;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class CachedFileAvailableFilter : ILinkFilter
{
    public required ISessionUtils SessionUtils { get; init; }

    public required Cached? CachedValue { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        if (CachedValue == Cached.Cached)
        {
            return links.Where(x => SessionUtils.GetCachedFileForLink(x) != null);
        }
        else if (CachedValue == Cached.NotCached)
        {
            return links.Where(x => SessionUtils.GetCachedFileForLink(x) == null);
        }
        return links;        
    }
}
