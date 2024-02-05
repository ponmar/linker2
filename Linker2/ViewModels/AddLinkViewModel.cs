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

namespace Linker2.ViewModels;

public partial class AddLinkViewModel : ObservableObject
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
            !string.IsNullOrEmpty(settingsRepo.Settings.DefaultTag))
        {
            LinkTags = settingsRepo.Settings.DefaultTag;
        }

        HasLinkUrl = LinkDtoValidator.IsUrl(value);

        ValidateInput();
    }

    [ObservableProperty]
    private bool hasLinkUrl;

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

    private List<string> linkThumbnailUrls = [];

    partial void OnLinkThumbnailUrlChanged(string value)
    {
        var index = linkThumbnailUrls.IndexOf(value);
        LinkThumbnailUrlIndexText = index == -1 ? "" : $"{index + 1}/{linkThumbnailUrls.Count}";
        ValidateInput();
    }

    public bool HasNextThumbnailUrl
    {
        get
        {
            var index = linkThumbnailUrls.IndexOf(LinkThumbnailUrl);
            return index != -1 && index < linkThumbnailUrls.Count - 1;
        }
    }

    public bool HasPreviousThumbnailUrl
    {
        get
        {
            var index = linkThumbnailUrls.IndexOf(LinkThumbnailUrl);
            return index > 0;
        }
    }

    [ObservableProperty]
    private string linkTags = string.Empty;

    partial void OnLinkTagsChanged(string value)
    {
        ValidateInput();
    }

    public bool ClearRatingEnabled => LinkRating is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClearRatingEnabled))]
    private int? linkRating;

    public bool EditingLink => linkToEdit is not null;
    private readonly LinkDto? linkToEdit;

    [ObservableProperty]
    private bool savePossible;

    private readonly IDialogs dialogs;
    private readonly ILinkModification linkModification;
    private readonly ILinkRepository linkRepository;
    private readonly IUrlDataFetcher urlDataFetcher;
    private readonly ISettingsRepository settingsRepo;

    public AddLinkViewModel(IDialogs dialogs, ILinkModification linkModification, ILinkRepository linkRepository, IUrlDataFetcher urlDataFetcher, ISettingsRepository settingsRepo, LinkDto? linkToEdit = null)
    {
        this.dialogs = dialogs;
        this.linkModification = linkModification;
        this.linkRepository = linkRepository;
        this.urlDataFetcher = urlDataFetcher;
        this.settingsRepo = settingsRepo;
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
    private void FetchLinkData()
    {
        TrimInput();

        if (!EditingLink && linkRepository.Links.Any(x => x.Url == LinkUrl))
        {
            dialogs.ShowErrorDialog("Link already added");
            return;
        }

        urlDataFetcher.LoadDataFromUrl(LinkUrl, out var loadedTitle, out var loadedThumbnailUrls);
        if (loadedTitle is not null)
        {
            LinkTitle = loadedTitle;
        }
        if (loadedThumbnailUrls.Count > 0)
        {
            linkThumbnailUrls = loadedThumbnailUrls;
            LinkThumbnailUrl = loadedThumbnailUrls.First();
        }
        else
        {
            LinkThumbnailUrl = string.Empty;
        }

        LinkTitle = loadedTitle ?? string.Empty;

        if (string.IsNullOrEmpty(LinkTags))
        {
            LinkTags = settingsRepo.Settings.DefaultTag;
        }

        if (!EditingLink && LinkTags == settingsRepo.Settings.DefaultTag)
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
    private void SaveLink()
    {
        TrimInput();

        // Note: needs re-validation after input has been trimmed
        var validationResult = ValidateInput();
        if (!validationResult.IsValid)
        {
            dialogs.ShowErrorDialog(validationResult);
            return;
        }

        if (!EditingLink && linkRepository.Links.Any(x => x.Url == LinkUrl))
        {
            dialogs.ShowErrorDialog("Link already added");
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

        Messenger.Send(new CloseDialog());
    }

    private LinkDto InputToDto(List<string> tags)
    {
        var openCounter = linkToEdit is not null ? linkToEdit.OpenCounter : 0;
        var dateTime = linkToEdit is not null ? linkToEdit.DateTime : DateTime.Now;
        return new LinkDto(LinkTitle, tags, LinkUrl, dateTime, LinkRating, string.IsNullOrEmpty(LinkThumbnailUrl) ? null : LinkThumbnailUrl, openCounter);
    }

    [RelayCommand]
    private void NextThumbnailUrl()
    {
        var index = linkThumbnailUrls.IndexOf(LinkThumbnailUrl);
        if (index != -1 && index < linkThumbnailUrls.Count - 1)
        {
            index++;
            LinkThumbnailUrl = linkThumbnailUrls[index];
        }
    }

    [RelayCommand]
    private void PreviousThumbnailUrl()
    {
        var index = linkThumbnailUrls.IndexOf(LinkThumbnailUrl);
        if (index > 0)
        {
            index--;
            LinkThumbnailUrl = linkThumbnailUrls[index];
        }
    }
}
