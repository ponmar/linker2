using System;
using System.Collections.Generic;

namespace Linker2.Configuration;

public enum OrderBy
{
    Rating,
    Time,
    Title,
    Random,
    Views,
    Tags,
}

public enum Cached
{
    Cached,
    NotCached,
}

public record SettingsDto(
    string OpenLinkCommand,
    string OpenLinkArguments,
    string DefaultTag,
    int LockAfterSeconds,
    string? GeckoDriverPath,
    List<string> ThumbnailImageIds,
    bool ShowDetails,
    bool ClearClipboardWhenSessionStops,
    bool QuitWhenSessionTimeouts,
    bool DeselectFileWhenSessionTimeouts,
    string? CachedFileDirectoryPath);

public record LinkDto(
    string? Title,
    List<string> Tags,
    string Url,
    DateTime DateTime,
    int? Rating,
    string? ThumbnailUrl,
    long OpenCounter);

public record FiltersDto(
    string? Text,
    string? Rating,
    string? Site,
    List<string> Tags,
    bool CombineTags,
    string? HideTags,
    OrderBy OrderBy,
    bool ReversedOrder,
    Cached? Cached);

public record DataDto(
    SettingsDto Settings,
    List<LinkDto> Links,
    FiltersDto Filters,
    string? SelectedUrl
);
