using Linker2.Model;
using System.Collections.Generic;

namespace Linker2.Filters;

public interface ILinkFilter
{
    IEnumerable<LinkDto> Apply(IEnumerable<LinkDto> links);
}
