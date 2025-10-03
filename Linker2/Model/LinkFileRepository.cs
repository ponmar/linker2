using System.Collections.Generic;
using System.IO;
using Linker2.Configuration;
using Linker2.Extensions;

namespace Linker2.Model;

public interface ILinkFileRepository
{
    IEnumerable<string> LinkFilePaths { get; }
    string? GetLinkFilePath(LinkDto link);
    void Update(IEnumerable<string> newLinkFiles);
    void Clear();
}

public class LinkFileRepository : ILinkFileRepository
{
    public IEnumerable<string> LinkFilePaths => linkFiles;

    private readonly List<string> linkFiles = [];

    public string? GetLinkFilePath(LinkDto link)
    {
        if (link.Title.HasContent())
        {
            var expectedFilenameWithoutExtension = string.Join("_", link.Title!.Split(Path.GetInvalidFileNameChars()));
            foreach (var linkFile in linkFiles)
            {
                if (expectedFilenameWithoutExtension == Path.GetFileNameWithoutExtension(linkFile))
                {
                    return linkFile;
                }
            }
        }
        return null;
    }

    public void Update(IEnumerable<string> newLinkFiles)
    {
        linkFiles.Clear();
        linkFiles.AddRange(newLinkFiles);
        Messenger.Send(new LinkFileRepositoryUpdated());
    }

    public void Clear()
    {
        linkFiles.Clear();
        Messenger.Send(new LinkFileRepositoryUpdated());
    }
}
