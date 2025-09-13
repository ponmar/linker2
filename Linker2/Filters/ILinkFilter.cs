using Linker2.Configuration;
using System.Collections.Generic;

namespace Linker2.Filters;

public interface ILinkFilter
{
    IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links);
}
