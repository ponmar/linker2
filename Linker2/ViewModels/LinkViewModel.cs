using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Configuration;
using Linker2.Model;
using Linker2.Validators;
using Linker2.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Linker2.ViewModels;

public partial class LinkTagViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private bool isChecked;

    public LinkTagViewModel(string name, bool isChecked)
    {
        Name = name;
        IsChecked = isChecked;
    }
}

public partial class LinkViewModel : ObservableObject
{
    private const int OriginalImageWidth = 320;
    private const int OriginalImageHeight = 180;
    private const int ShortTitleLength = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailWidth))]
    [NotifyPropertyChangedFor(nameof(ThumbnailHeight))]
    [NotifyPropertyChangedFor(nameof(FontSize))]
    [NotifyPropertyChangedFor(nameof(RatingFontSize))]
    private bool showDetails;

    public int ThumbnailWidth => ShowDetails ? OriginalImageWidth / 3 : OriginalImageWidth;

    public int ThumbnailHeight => ShowDetails ? OriginalImageHeight / 3 : OriginalImageHeight;

    [ObservableProperty]
    private bool fileExists;

    public int FontSize => ShowDetails ? 12 : 18;
    public int RatingFontSize => ShowDetails ? 20 : 30;

    [ObservableProperty]
    private string longTitle = string.Empty;

    [ObservableProperty]
    private string shortTitle = string.Empty;

    public string VisualizedDateTime => LinkDto.DateTime.ToString("yyyy-MM-dd HH:mm");
    public string Url => LinkDto.Url;
    public int? Rating => LinkDto.Rating;
    public long OpenCounter => LinkDto.OpenCounter;
    public string RatingDescription => LinkDto.Rating is null ? Constants.NotRatedFilterText : $"Rated {LinkDto.Rating} / {LinkDtoValidator.MaxLinkRating}";
    public string ThumbnailUrl => LinkDto.ThumbnailUrl ?? string.Empty;
    public bool HasThumbnailUrl => !string.IsNullOrEmpty(LinkDto.ThumbnailUrl);

    [ObservableProperty]
    private Bitmap? thumbnailImage;

    [ObservableProperty]
    private ObservableCollection<LinkTagViewModel> tags = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RatingFontSize))]
    [NotifyPropertyChangedFor(nameof(VisualizedDateTime))]
    [NotifyPropertyChangedFor(nameof(Url))]
    [NotifyPropertyChangedFor(nameof(Rating))]
    [NotifyPropertyChangedFor(nameof(OpenCounter))]
    [NotifyPropertyChangedFor(nameof(RatingDescription))]
    [NotifyPropertyChangedFor(nameof(ThumbnailUrl))]
    [NotifyPropertyChangedFor(nameof(HasThumbnailUrl))]
    private LinkDto linkDto;

    partial void OnLinkDtoChanged(LinkDto value)
    {
        UpdateTitles();
        UpdateTags();
        UpdateThumbnailImage();
    }

    private readonly ImageCache imageCache;

    public LinkViewModel(LinkDto linkDto, IEnumerable<string> selectedTags, bool showDetails, ImageCache imageCache)
    {
        this.imageCache = imageCache;
        this.linkDto = linkDto;
        OnLinkDtoChanged(linkDto);

        ShowDetails = showDetails;

        UpdateThumbnailImage();
        UpdateSelectedTags(selectedTags);
        UpdateTitles();
    }

    private void UpdateThumbnailImage()
    {
        if (LinkDto.ThumbnailUrl is not null)
        {
            try
            {
                ThumbnailImage = imageCache.Add(LinkDto.ThumbnailUrl, LinkDto.ThumbnailUrl);
                return;
            }
            catch
            {
            }
        }
        ThumbnailImage = null;
    }

    private void UpdateTitles()
    {
        LongTitle = LinkDto.Title ?? "-";

        if (LinkDto.Title is null)
        {
            ShortTitle = "-";
        }
        else if (LinkDto.Title.Length < ShortTitleLength)
        {
            ShortTitle = LinkDto.Title;
        }
        else
        {
            ShortTitle = LinkDto.Title[..ShortTitleLength] + "...";
        }
    }

    private void UpdateTags()
    {
        var selectedTags = Tags.Where(x => x.IsChecked).Select(x => x.Name).ToList();
        Tags.Clear();
        foreach (var tag in LinkDto.Tags.Select(x => new LinkTagViewModel(x, selectedTags.Contains(x))))
        {
            Tags.Add(tag);
        }
    }

    public void UpdateSelectedTags(IEnumerable<string> selectedTags)
    {
        foreach (var tag in Tags)
        {
            tag.IsChecked = selectedTags.Contains(tag.Name);
        }
    }

    [RelayCommand]
    private static void OpenLink(LinkDto link) => Messenger.Send(new OpenLink(link));

    [RelayCommand]
    private static void LocateLinkFile(LinkDto link) => Messenger.Send(new LocateLinkFile(link));

    [RelayCommand]
    private static void CopyFilePath(LinkDto link) => Messenger.Send(new CopyLinkFilePath(link));

    [RelayCommand]
    private static void CopyLinkUrl(LinkDto link) => Messenger.Send(new CopyLinkUrl(link));

    [RelayCommand]
    private static void CopyLinkTitle(LinkDto link) => Messenger.Send(new CopyLinkTitle(link));

    [RelayCommand]
    private static void AddLink() => Messenger.Send<StartAddLink>();

    [RelayCommand]
    private static void EditLink(LinkDto link) => Messenger.Send(new StartEditLink(link));

    [RelayCommand]
    private static void RemoveLink(LinkDto link) => Messenger.Send(new StartRemoveLink(link));

    [RelayCommand]
    private void OpenLinkThumbnail(LinkDto link) => Messenger.Send(new OpenLinkThumbnail(link));
}
