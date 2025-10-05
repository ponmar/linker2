using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Model;
using Linker2.Validators;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using Linker2.HttpHelpers;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using Linker2.Configuration;

namespace Linker2.ViewModels.Dialogs;

public partial class AddOrEditLinkViewModel : ObservableObject
{
    [GeneratedRegex("[^a-zA-Z0-9 -]")]
    private static partial Regex TagRegex();

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string linkUrl = string.Empty;

    partial void OnLinkUrlChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && string.IsNullOrEmpty(LinkTags) &&
            !string.IsNullOrEmpty(settingsProvider.Settings.DefaultTag))
        {
            LinkTags = settingsProvider.Settings.DefaultTag;
        }

        ValidateInput();
    }

    [ObservableProperty]
    private string linkTitle = string.Empty;

    partial void OnLinkTitleChanged(string value)
    {
        ValidateInput();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkThumbnailUrlBitmap))]
    [NotifyPropertyChangedFor(nameof(HasNextThumbnailUrl))]
    [NotifyPropertyChangedFor(nameof(HasPreviousThumbnailUrl))]
    private string linkThumbnailUrl = string.Empty;

    public Task<Bitmap?> LinkThumbnailUrlBitmap => ImageHelper.LoadFromWeb(LinkThumbnailUrl);

    [ObservableProperty]
    private string linkThumbnailUrlIndexText = string.Empty;

    public ObservableCollection<string> LinkThumbnailUrls { get; } = [];

    partial void OnLinkThumbnailUrlChanged(string value)
    {
        var index = LinkThumbnailUrls.IndexOf(value);
        LinkThumbnailUrlIndexText = index == -1 ? "" : $"{index + 1}/{LinkThumbnailUrls.Count}";
        ValidateInput();
    }

    public bool HasNextThumbnailUrl
    {
        get
        {
            var index = LinkThumbnailUrls.IndexOf(LinkThumbnailUrl);
            return index != -1 && index < LinkThumbnailUrls.Count - 1;
        }
    }

    public bool HasPreviousThumbnailUrl
    {
        get
        {
            var index = LinkThumbnailUrls.IndexOf(LinkThumbnailUrl);
            return index > 0;
        }
    }

    [ObservableProperty]
    private string linkTags = string.Empty;

    partial void OnLinkTagsChanged(string value)
    {
        ValidateInput();
    }

    [ObservableProperty]
    private int? linkRating;

    public bool EditingLink => linkToEdit is not null;
    private readonly LinkDto? linkToEdit;

    [ObservableProperty]
    private bool savePossible;

    private readonly IFileSystem fileSystem;
    private readonly IDialogs dialogs;
    private readonly ILinkModification linkModification;
    private readonly ILinkRepository linkRepository;
    private readonly IWebPageScraperProvider webPageScraperProvider;
    private readonly ISettingsProvider settingsProvider;

    public AddOrEditLinkViewModel(IFileSystem fileSystem, IDialogs dialogs, ILinkModification linkModification, ILinkRepository linkRepository, IWebPageScraperProvider webPageScraperProvider, ISettingsProvider settingsProvider, IClipboardService clipboardService, LinkDto? linkToEdit = null)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.linkModification = linkModification;
        this.linkRepository = linkRepository;
        this.webPageScraperProvider = webPageScraperProvider;
        this.settingsProvider = settingsProvider;
        this.linkToEdit = linkToEdit;

        if (linkToEdit is not null)
        {
            title = "Edit link";
            LinkUrl = linkToEdit.Url;
            LinkTags = linkToEdit.Tags is null ? string.Empty : string.Join(',', linkToEdit.Tags);
            LinkTitle = linkToEdit.Title ?? string.Empty;
            LinkRating = linkToEdit.Rating;
            LinkThumbnailUrl = linkToEdit.ThumbnailUrl ?? string.Empty;
        }
        else
        {
            title = "Add link";
            LinkUrl = string.Empty;
            try
            {
                var clipboardText = clipboardService.GetTextAsync().Result;
                if (clipboardText is not null)
                {
                    _ = new Uri(clipboardText);
                    LinkUrl = clipboardText;
                }
            }
            catch
            {
                // ignore
            }
            LinkTags = string.Empty;
            LinkTitle = string.Empty;
            LinkRating = null;
            LinkThumbnailUrl = string.Empty;
        }
    }

    [RelayCommand]
    private void SetRating(string rating)
    {
        LinkRating = int.Parse(rating);
    }

    [RelayCommand]
    private void ClearRating()
    {
        LinkRating = null;
    }

    private void TrimInput()
    {
        LinkUrl = LinkUrl.Trim();
        LinkThumbnailUrl = LinkThumbnailUrl.Trim();
        LinkTitle = LinkTitle.Trim();
        LinkTags = LinkTags.Trim();

        var tags = new List<string>();
        foreach (var tag in LinkTags.Split(','))
        {
            var trimmed = tag.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                tags.Add(tag.Trim());
            }
        }
        LinkTags = string.Join(",", tags);
    }

    [RelayCommand]
    private void FetchLinkDataViaFirefox()
    {
        if (string.IsNullOrEmpty(settingsProvider.Settings.GeckoDriverPath))
        {
            dialogs.ShowErrorDialogAsync($"Geckor driver directory not configured");
            return;
        }

        if (!fileSystem.Directory.Exists(settingsProvider.Settings.GeckoDriverPath))
        {
            dialogs.ShowErrorDialogAsync($"Gecko driver directory not found: {settingsProvider.Settings.GeckoDriverPath}");
            return;
        }

        if (webPageScraperProvider.Firefox is null)
        {
            try
            {
                webPageScraperProvider.Firefox = new FirefoxWebPageScraper(settingsProvider.Settings.GeckoDriverPath, true);
            }
            catch (Exception e)
            {
                dialogs.ShowErrorDialogAsync($"Reverting to default web page scraper due to exception when creating FirefoxWebPageScraper: {e.Message}");
                return;
            }
        }

        FetchLinkData(webPageScraperProvider.Firefox);
    }

    [RelayCommand]
    private void FetchLinkDataViaHtmlAgilityPack()
    {
        FetchLinkData(webPageScraperProvider.HtmlAgilityPack);
    }

    private void FetchLinkData(IWebPageScraper webPageScraper)
    {
        TrimInput();

        if (!EditingLink && linkRepository.Links.Any(x => x.Url == LinkUrl))
        {
            dialogs.ShowErrorDialogAsync("Link already added");
            return;
        }

        if (!webPageScraper.Load(LinkUrl))
        {
            dialogs.ShowErrorDialogAsync("Load error");
            return;
        }

        var loadedTitle = webPageScraper!.PageTitle;
        var loadedThumbnailUrls = webPageScraper.GetImageSrcs(settingsProvider.Settings.ThumbnailImageIds);    
        if (loadedTitle is not null)
        {
            LinkTitle = loadedTitle;
        }
        if (loadedThumbnailUrls.Count > 0)
        {
            LinkThumbnailUrls.Clear();
            loadedThumbnailUrls.ForEach(x => LinkThumbnailUrls.Add(x));
            LinkThumbnailUrl = loadedThumbnailUrls.First();
        }
        else
        {
            LinkThumbnailUrl = string.Empty;
        }

        LinkTitle = loadedTitle ?? string.Empty;

        if (string.IsNullOrEmpty(LinkTags))
        {
            LinkTags = settingsProvider.Settings.DefaultTag;
        }

        if (!EditingLink && LinkTags == settingsProvider.Settings.DefaultTag)
        {
            var tagsFromTitle = new List<string>();
            var existingTags = linkRepository.Links.SelectMany(x => x.Tags).Distinct();
            var rgx = TagRegex();
            foreach (var word in LinkTitle.Split(" "))
            {
                var simplifiedWord = word.Trim();
                simplifiedWord = rgx.Replace(simplifiedWord, "");
                var matchingTag = existingTags.FirstOrDefault(x => x.Equals(simplifiedWord, StringComparison.OrdinalIgnoreCase));
                if (matchingTag is not null)
                {
                    tagsFromTitle.Add(matchingTag);
                }
            }

            if (!string.IsNullOrEmpty(LinkTags))
            {
                tagsFromTitle.Insert(0, LinkTags);
            }
            LinkTags = string.Join(",", tagsFromTitle);
        }

        var parsedTags = LinkTags.Split(",").Distinct().ToList();
        parsedTags.Sort();
        LinkTags = string.Join(",", parsedTags);
    }

    private ValidationResult ValidateInput()
    {
        var tags = LinkTags.Split(",").ToList();
        var link = InputToDto(tags);
        var validator = new LinkDtoValidator();
        var validationResult = validator.Validate(link);
        SavePossible = validationResult.IsValid;
        return validationResult;
    }

    [RelayCommand]
    private async Task SaveLink()
    {
        TrimInput();

        // Note: needs re-validation after input has been trimmed
        var validationResult = ValidateInput();
        if (!validationResult.IsValid)
        {
            await dialogs.ShowErrorDialogAsync(validationResult);
            return;
        }

        if (!EditingLink && linkRepository.Links.Any(x => x.Url == LinkUrl))
        {
            await dialogs.ShowErrorDialogAsync("Link already added");
            return;
        }

        var sortedTags = LinkTags.Split(',').ToList();
        sortedTags.Sort();

        var link = InputToDto(sortedTags);

        if (EditingLink)
        {
            linkModification.UpdateLink(link);
        }
        else
        {
            linkModification.AddLink(link);
        }

        Messenger.Send<CloseDialog>();
    }

    private LinkDto InputToDto(List<string> tags)
    {
        var dateTime = linkToEdit is not null ? linkToEdit.DateTime : DateTime.Now;
        return new LinkDto(LinkTitle, tags, LinkUrl, dateTime, LinkRating, string.IsNullOrEmpty(LinkThumbnailUrl) ? null : LinkThumbnailUrl);
    }

    [RelayCommand]
    private void NextThumbnailUrl()
    {
        var index = LinkThumbnailUrls.IndexOf(LinkThumbnailUrl);
        if (index != -1 && index < LinkThumbnailUrls.Count - 1)
        {
            index++;
            LinkThumbnailUrl = LinkThumbnailUrls[index];
        }
    }

    [RelayCommand]
    private void PreviousThumbnailUrl()
    {
        var index = LinkThumbnailUrls.IndexOf(LinkThumbnailUrl);
        if (index > 0)
        {
            index--;
            LinkThumbnailUrl = LinkThumbnailUrls[index];
        }
    }
}
