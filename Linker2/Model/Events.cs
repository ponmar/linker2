using Linker2.Configuration;

namespace Linker2.Model;

public record SessionStarted(Session Session);
public record SessionTick(Session Session);
public record SessionStopping;
public record SessionStopped(SettingsDto Settings);
public record LinkFileRepositoryUpdated();

public record SettingsUpdated();
public record DataUpdatedChanged();

public record LinkAdded(Session Session, LinkDto Link);
public record LinkUpdated(Session Session, LinkDto Link);
public record LinkRemoved(Session Session, LinkDto Link);

public record LinkSelected(LinkDto Link);
public record LinkDeselected;
public record OpenLink(LinkDto Link);
public record LocateLinkFile(LinkDto Link);
public record CopyLinkFilePath(LinkDto Link);
public record CopyLinkUrl(LinkDto Link);
public record CopyLinkTitle(LinkDto Link);
public record StartEditLink(LinkDto Link);
public record StartAddLink();
public record StartRemoveLink(LinkDto Link);

public record CloseDialog;