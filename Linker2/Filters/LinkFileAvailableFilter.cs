using Linker2.Configuration;
using Linker2.Model;
using System.Collections.Generic;
using System.Linq;

namespace Linker2.Filters;

public class LinkFileAvailableFilter : ILinkFilter
{
    public required ILinkFileRepository LinkFileRepo { get; init; }

    public required LinkFileAvailability? CachedValue { get; init; }

    public IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links)
    {
        if (CachedValue == LinkFileAvailability.Available)
        {
            return links.Where(x => LinkFileRepo.LinkFileExists(x));
        }
        else if (CachedValue == LinkFileAvailability.NotAvailable)
        {
            return links.Where(x => !LinkFileRepo.LinkFileExists(x));
        }
        return links;        
    }
}
