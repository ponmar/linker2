using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Configuration;
using Linker2.Filters;
using Linker2.Model;
using Linker2.Validators;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Linker2.ViewModels;

public partial class TagFilterViewModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    private bool isChecked;

    public TagFilterViewModel(string name, bool isChecked)
    {
        Name = name;
        IsChecked = isChecked;
    }
}

public partial class LinksViewModel : ObservableObject
{
    private static readonly Random random = new();

    private List<LinkViewModel> allLinks = [];

    public ObservableCollection<LinkViewModel> Links { get; private set; } = [];

    public ObservableCollection<OrderBy> OrderByValues { get; } = [];

    [ObservableProperty]
    private OrderBy selectedOrderBy = OrderBy.Rating;

    partial void OnSelectedOrderByChanged(OrderBy value)
    {
        SortLinks();
    }

    public ObservableCollection<LinkFileAvailability> CachedValues { get; } = [];

    [ObservableProperty]
    private LinkFileAvailability? selectedCachedValue;

    partial void OnSelectedCachedValueChanged(LinkFileAvailability? value)
    {
        UpdateLinks();
    }

    [ObservableProperty]
    private bool combineTagFilters;

    partial void OnCombineTagFiltersChanged(bool value)
    {
        if (TagFilters.Any(x => x.IsChecked))
        {
            UpdateLinks();
        }
    }

    public ObservableCollection<string> SiteFilteringValues { get; } = [];

    [ObservableProperty]
    private string? selectedSiteFiltering;

    partial void OnSelectedSiteFilteringChanged(string? value)
    {
        UpdateLinks();
    }

    public ObservableCollection<string> RatingFilteringValues { get; } = [];

    [ObservableProperty]
    private string? selectedRatingFiltering;

    partial void OnSelectedRatingFilteringChanged(string? value)
    {
        UpdateLinks();
    }

    [ObservableProperty]
    private ObservableCollection<TagFilterViewModel> tagFilters = [];

    [ObservableProperty]
    private bool reversedOrder = false;

    partial void OnReversedOrderChanged(bool value)
    {
        UpdateLinks();
    }

    [ObservableProperty]
    private string filterText = string.Empty;

    partial void OnFilterTextChanged(string value)
    {
        UpdateLinks();
    }

    [ObservableProperty]
    private string hiddenTagsText = string.Empty;

    partial void OnHiddenTagsTextChanged(string value)
    {
        UpdateLinks();
    }

    [ObservableProperty]
    private string filtersHeading = string.Empty;

    [ObservableProperty]
    private LinkViewModel? selectedLink;

    partial void OnSelectedLinkChanging(LinkViewModel? value)
    {
        if (SelectedLink is not null)
        {
            Messenger.Send<LinkDeselected>();
        }
    }

    partial void OnSelectedLinkChanged(LinkViewModel? value)
    {
        if (SelectedLink is not null)
        {
            Messenger.Send(new LinkSelected(SelectedLink.LinkDto));
        }
    }

    public bool SessionOngoing => Session is not null;

    private Session? Session
    {
        get => session;
        set
        {
            session = value;
            OnPropertyChanged(nameof(SessionOngoing));
        }
    }
    private Session? session = null;

    private readonly ISessionSaver sessionSaver;
    private readonly ISessionUtils sessionUtils;
    private readonly ILinkFileRepository linkFileRepo;

    public LinksViewModel(ISessionSaver sessionSaver, ISessionUtils sessionUtils, ILinkFileRepository linkFileRepo)
    {
        this.sessionSaver = sessionSaver;
        this.sessionUtils = sessionUtils;
        this.linkFileRepo = linkFileRepo;

        foreach (var orderByValue in Enum.GetValues<OrderBy>())
        {
            OrderByValues.Add(orderByValue);
        }

        foreach (var cachedValue in Enum.GetValues<LinkFileAvailability>())
        {
            CachedValues.Add(cachedValue);
        }

        this.RegisterForEvent<SessionStarted>((x) =>
        {
            Session = x.Session;

            ReloadAvailableSites();
            ReloadAvailableRatings();
            ReloadAvailableTags();
            InitFilters();
            InitLinks();
            InitSelection();
        });

        this.RegisterForEvent<SessionStopping>((x) =>
        {
            var selectedUrl = selectedLink?.Url;
            if (selectedUrl != Session!.Data.SelectedUrl)
            {
                sessionSaver.SaveSelection(selectedUrl);
            }

            var tags = tagFilters.Where(x => x.IsChecked).Select(x => x.Name).ToList();
            var text = string.IsNullOrEmpty(filterText) ? null : filterText;
            var hideTags = string.IsNullOrEmpty(hiddenTagsText) ? null : hiddenTagsText;
            var filters = new FiltersDto(text, SelectedRatingFiltering, SelectedSiteFiltering, tags, combineTagFilters, hideTags, SelectedOrderBy, ReversedOrder, SelectedCachedValue);

            var prevFiltersJson = JsonConvert.SerializeObject(Session!.Data.Filters);
            var filtersJson = JsonConvert.SerializeObject(filters);
            if (prevFiltersJson != filtersJson)
            {
                // Validation error will not popup a dialog that keeps the session running
                var filtersValidator = new FiltersDtoValidator();
                var validationResult = filtersValidator.Validate(filters);
                if (validationResult.IsValid)
                {
                    try
                    {
                        sessionSaver.SaveFilters(filters);
                    }
                    catch
                    {
                        // Ignore error to be able to stop the session
                    }
                }
            }
        });

        this.RegisterForEvent<SessionStopped>((x) =>
        {
            Session = null;
            SiteFilteringValues.Clear();
            RatingFilteringValues.Clear();
            TagFilters.Clear();
            allLinks.Clear();
            Links.Clear();
            FilterText = string.Empty;
            HiddenTagsText = string.Empty;
            UpdateLinks();
        });

        this.RegisterForEvent<LinkAdded>((m) =>
        {
            var selectedTags = tagFilters.Where(x => x.IsChecked).Select(x => x.Name);
            var minimize = session!.Data.Settings.ShowDetails;
            allLinks.Add(new LinkViewModel(m.Link, selectedTags, minimize, session!.ImageCache) { FileExists = linkFileRepo.GetLinkFilePath(m.Link) != null });
            ReloadAvailableTags();
            UpdateLinks();

            SelectedLink = Links.FirstOrDefault(x => x.LinkDto.Url == m.Link.Url);
        });

        this.RegisterForEvent<LinkUpdated>((m) =>
        {
            SelectedLink = null;

            var updatedLink = allLinks.First(x => x.Url == m.Link.Url);
            updatedLink.LinkDto = m.Link;
            var selectedTags = tagFilters.Where(x => x.IsChecked).Select(x => x.Name);
            updatedLink.UpdateSelectedTags(selectedTags);

            ReloadAvailableSites();
            ReloadAvailableRatings();
            ReloadAvailableTags();

            UpdateLinks();

            SelectedLink = Links.FirstOrDefault(x => x.LinkDto.Url == m.Link.Url);
        });

        this.RegisterForEvent<LinkRemoved>((m) =>
        {
            allLinks.RemoveAll(x => x.Url == m.Link.Url);

            // Not always a hit because removed link may be filtered
            int selectedLinkIndex = -1;
            foreach (var link in Links)
            {
                if (link.LinkDto.Url == m.Link.Url)
                {
                    selectedLinkIndex = Links.IndexOf(link);
                    Links.Remove(link);
                    break;
                }
            }

            ReloadAvailableSites();
            ReloadAvailableRatings();
            ReloadAvailableTags();

            UpdateLinks();

            // Select item after removed item in list
            if (selectedLinkIndex != -1 && selectedLinkIndex < Links.Count)
            {
                SelectedLink = Links[selectedLinkIndex];
            }
        });

        this.RegisterForEvent<SettingsUpdated>((m) =>
        {
            allLinks.ForEach(x => x.ShowDetails = session!.Data.Settings.ShowDetails);
        });

        this.RegisterForEvent<LinkFileRepositoryUpdated>((m) =>
        {
            allLinks.ForEach(x => x.FileExists = linkFileRepo.GetLinkFilePath(x.LinkDto) != null);
        });

        UpdateLinks();
    }

    private void ReloadAvailableSites()
    {
        var urlHostnames = session!.Data.Links.Select(x => new Uri(x.Url).Host).Distinct().ToList();
        urlHostnames.Sort();
        if (!SiteFilteringValues.SequenceEqual(urlHostnames))
        {
            SiteFilteringValues.Clear();
            urlHostnames.ForEach(x => SiteFilteringValues.Add(x));
        }
    }

    private void ReloadAvailableRatings()
    {
        var ratings = session!.Data.Links.Where(x => x.Rating is not null).Select(x => x.Rating.ToString()).Distinct().ToList();
        ratings.Sort();
        ratings.Reverse();
        if (session.Data.Links.Any(x => x.Rating is null))
        {
            ratings.Add(Constants.NotRatedFilterText);
        }
        if (!RatingFilteringValues.SequenceEqual(ratings))
        {
            RatingFilteringValues.Clear();
            ratings.ForEach(x => RatingFilteringValues.Add(x!));
        }
    }

    private void ReloadAvailableTags()
    {
        var preSelectedTags = TagFilters.Where(x => x.IsChecked).Select(x => x.Name).ToList();

        var updatedTags = session!.Data.Links.SelectMany(x => x.Tags).Distinct().ToList();
        updatedTags.Sort();

        TagFilters.Clear();
        updatedTags.ForEach(x => TagFilters.Add(new(x, preSelectedTags.Contains(x))));

        OnPropertyChanged(nameof(TagFilters));
    }

    private void InitLinks()
    {
        var selectedTags = TagFilters.Where(x => x.IsChecked).Select(x => x.Name);
        var minimize = session!.Data.Settings.ShowDetails;
        allLinks = session!.Data.Links.Select(x => new LinkViewModel(x, selectedTags, minimize, session!.ImageCache) { FileExists = linkFileRepo.GetLinkFilePath(x) != null }).ToList();

        foreach (var linkVm in allLinks)
        {
            Links.Add(linkVm);
        }

        UpdateLinks();
    }

    private void InitSelection()
    {
        if (Session!.Data.SelectedUrl is not null)
        {
            SelectedLink = Links.First(x => x.Url == Session.Data.SelectedUrl);
        }
    }

    private void InitFilters()
    {
        FilterText = session!.Data!.Filters.Text ?? string.Empty;
        HiddenTagsText = session!.Data!.Filters.HideTags ?? string.Empty;

        foreach (var tagFilter in TagFilters)
        {
            tagFilter.IsChecked = session!.Data.Filters.Tags.Contains(tagFilter.Name);
            foreach (var link in Links)
            {
                var linkTag = link.Tags.FirstOrDefault(x => x.Name == tagFilter.Name);
                if (linkTag is not null)
                {
                    linkTag.IsChecked = tagFilter.IsChecked;
                }
            }
        }

        CombineTagFilters = session!.Data!.Filters.CombineTags;

        SelectedSiteFiltering = session!.Data.Filters.Site;

        SelectedRatingFiltering = session!.Data.Filters.Rating?.ToString();

        SelectedOrderBy = session!.Data.Filters.OrderBy;

        ReversedOrder = session!.Data.Filters.ReversedOrder;
    }

    private void UpdateLinks()
    {
        var filteredLinks = ApplyLinkFilters();
        UpdateCollection(filteredLinks);
        SortLinks();
        FiltersHeading = $"Filters [{Links.Count} / {(session is not null ? session.Data.Links.Count : 0)}]";
    }

    private IEnumerable<LinkViewModel> ApplyLinkFilters()
    {
        var checkedTags = TagFilters.Where(x => x.IsChecked).Select(x => x.Name);
        var noTags = new List<string>();

        var linkFilters = new List<ILinkFilter>()
        {
            new HiddenTagsFilter() { HiddenTags = HiddenTagsText.Split(",") },
            new CombinedTagFilter() { Tags = CombineTagFilters ? checkedTags : noTags },
            new AnyTagFilter() { Tags = CombineTagFilters ? noTags : checkedTags },
            new RatingFilter() { Rating = RatingSelectionToRating() },
            new NoRatingFilter() { Enabled = SelectedRatingFiltering == Constants.NotRatedFilterText },
            new SiteFilter() { Site = SelectedSiteFiltering },
            new TextFilter() { Text = FilterText.Trim() },
            new LinkFileAvailableFilter() { CachedValue = SelectedCachedValue, LinkFileRepo = linkFileRepo },
        };

        var filterdLinks = allLinks.Select(x => x.LinkDto);
        linkFilters.ForEach(x => filterdLinks = x.Apply(filterdLinks));
        return filterdLinks.Select(x => allLinks.First(y => y.LinkDto == x));
    }

    private int? RatingSelectionToRating()
    {
        if (SelectedRatingFiltering is null || SelectedRatingFiltering == Constants.NotRatedFilterText)
        {
            return null;
        }

        return int.Parse(SelectedRatingFiltering);
    }

    private void UpdateCollection(IEnumerable<LinkViewModel> filteredLinks)
    {
        foreach (var link in allLinks)
        {
            if (filteredLinks.Contains(link))
            {
                if (!Links.Contains(link))
                {
                    Links.Add(link);
                }
            }
            else
            {
                Links.Remove(link);
            }
        }
    }

    private void SortLinks()
    {
        List<LinkViewModel> sortedLinks = new(Links);

        switch (SelectedOrderBy)
        {
            case OrderBy.Rating:
                sortedLinks = sortedLinks.OrderByDescending(x => x.LinkDto.Rating).
                    ThenBy(x => x.LongTitle).ToList();
                break;

            case OrderBy.Time:
                sortedLinks = sortedLinks.OrderByDescending(x => x.LinkDto.DateTime).ToList();
                break;

            case OrderBy.Title:
                sortedLinks = sortedLinks.OrderBy(x => x.LongTitle).ToList();
                break;

            case OrderBy.Random:
                sortedLinks = sortedLinks.OrderBy(a => random.Next()).ToList();
                break;

            case OrderBy.Views:
                sortedLinks = sortedLinks.OrderByDescending(x => x.LinkDto.OpenCounter).
                    ThenByDescending(x => x.LinkDto.Rating).
                    ThenBy(x => x.LongTitle).ToList();
                break;

            case OrderBy.Tags:
                sortedLinks = sortedLinks.OrderBy(x => x.LinkDto.Tags.FirstOrDefault()).
                    ThenBy(x => x.LongTitle).ToList();
                break;
        }

        if (ReversedOrder)
        {
            sortedLinks.Reverse();
        }

        var preSelectedLink = SelectedLink;
        Links.Clear();
        sortedLinks.ForEach(x => Links.Add(x));
        SelectedLink = preSelectedLink;
    }

    [RelayCommand]
    private void ClearTextFilter()
    {
        FilterText = string.Empty;
    }

    [RelayCommand]
    private void ClearHiddenTagsText()
    {
        HiddenTagsText = string.Empty;
    }

    [RelayCommand]
    private void ClearTagFilter()
    {
        foreach (var link in allLinks)
        {
            foreach (var linkTag in link.Tags)
            {
                linkTag.IsChecked = false;
            }
        }
        foreach (var tagModel in TagFilters)
        {
            tagModel.IsChecked = false;
        }
        UpdateLinks();
    }

    [RelayCommand]
    private void ClearSiteFilter()
    {
        SelectedSiteFiltering = null;
    }

    [RelayCommand]
    private void ClearCachedValueFilter()
    {
        SelectedCachedValue = null;
    }

    [RelayCommand]
    private void ClearRatingFilter()
    {
        SelectedRatingFiltering = null;
    }

    [RelayCommand]
    private void ToggleTagFilter(TagFilterViewModel tagFilterToToggle)
    {
        foreach (var linkVm in allLinks)
        {
            var macthingTag = linkVm.Tags.FirstOrDefault(x => x.Name == tagFilterToToggle.Name);
            if (macthingTag is not null)
            {
                macthingTag.IsChecked = tagFilterToToggle.IsChecked;
            }
        }

        TagFilterUpdated();
    }

    private void TagFilterUpdated()
    {
        UpdateLinks();
    }

    [RelayCommand]
    private void ToggleTagFilterByName(LinkTagViewModel tagToToggle)
    {
        foreach (var link in allLinks)
        {
            foreach (var linkTag in link.Tags)
            {
                if (linkTag.Name == tagToToggle.Name)
                {
                    linkTag.IsChecked = tagToToggle.IsChecked;
                }
            }
        }

        var tagFilteringModel = TagFilters.First(x => x.Name == tagToToggle.Name);
        tagFilteringModel.IsChecked = tagToToggle.IsChecked;
        TagFilterUpdated();
    }
}
