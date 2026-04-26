using StudyApp.Pages;

namespace StudyApp;

public partial class AppShell : ContentPage
{
    private static readonly Color ActiveStrokeColor = Color.FromArgb("#5E98D7");
    private static readonly Color ActiveFillColor = Color.FromArgb("#7CB8F6");
    private static readonly Color ActiveBackgroundColor = Color.FromArgb("#E6F1FF");
    private static readonly Color InactiveColor = Color.FromArgb("#708398");

    private enum AppTabKind
    {
        Home,
        Notes,
        Profile
    }

    private readonly HomePage _homePage;
    private readonly NotesPage _notesPage;
    private readonly ProfilePage _profilePage;
    private AppTabKind _currentTab = AppTabKind.Home;
    private bool _hasLoadedInitialHomeBanner;

    public AppShell(HomePage homePage, NotesPage notesPage, ProfilePage profilePage)
    {
        InitializeComponent();

        _homePage = homePage;
        _notesPage = notesPage;
        _profilePage = profilePage;

        PrepareContent(_homePage);
        PrepareContent(_notesPage);
        PrepareContent(_profilePage);

        ContentHost.Children.Add(_homePage);
        ContentHost.Children.Add(_notesPage);
        ContentHost.Children.Add(_profilePage);

        _homePage.IsVisible = true;
        _homePage.Opacity = 1;
        _homePage.InputTransparent = false;

        UpdateTabVisuals(AppTabKind.Home);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var refreshHomeBanner = !_hasLoadedInitialHomeBanner && _currentTab == AppTabKind.Home;
        await RefreshTabAsync(_currentTab, refreshHomeBanner);

        if (refreshHomeBanner)
        {
            _hasLoadedInitialHomeBanner = true;
        }
    }

    private async void OnHomeTabTapped(object? sender, TappedEventArgs e)
    {
        await SelectTabAsync(AppTabKind.Home);
    }

    private async void OnNotesTabTapped(object? sender, TappedEventArgs e)
    {
        await SelectTabAsync(AppTabKind.Notes);
    }

    private async void OnProfileTabTapped(object? sender, TappedEventArgs e)
    {
        await SelectTabAsync(AppTabKind.Profile);
    }

    /// <summary>
    /// Handles tab switching with animation and data refresh.
    /// </summary>
    private async Task SelectTabAsync(AppTabKind nextTab)
    {
        if (_currentTab == nextTab)
        {
            await AnimateTabButtonAsync(GetTabButton(nextTab));
            return;
        }

        var currentView = GetTabView(_currentTab);
        var nextView = GetTabView(nextTab);

        await RefreshTabAsync(nextTab, refreshHomeBanner: false);

        nextView.IsVisible = true;
        nextView.InputTransparent = false;
        nextView.Opacity = 0;
        nextView.TranslationY = 10;

        UpdateTabVisuals(nextTab);

        var transitionTasks = new List<Task>
        {
            nextView.FadeTo(1, 180, Easing.CubicOut),
            nextView.TranslateTo(0, 0, 180, Easing.CubicOut),
            AnimateTabButtonAsync(GetTabButton(nextTab))
        };

        if (currentView is not null)
        {
            transitionTasks.Add(currentView.FadeTo(0, 120, Easing.CubicIn));
        }

        await Task.WhenAll(transitionTasks);

        if (currentView is not null)
        {
            currentView.IsVisible = false;
            currentView.InputTransparent = true;
            currentView.Opacity = 1;
            currentView.TranslationY = 0;
        }

        _currentTab = nextTab;
    }

    /// <summary>
    /// Refresh tab data before display to avoid stale UI state.
    /// </summary>
    private async Task RefreshTabAsync(AppTabKind tab, bool refreshHomeBanner)
    {
        switch (tab)
        {
            case AppTabKind.Home:
                await _homePage.RefreshAsync(refreshHomeBanner);
                break;
            case AppTabKind.Notes:
                await _notesPage.RefreshAsync();
                break;
            case AppTabKind.Profile:
                await _profilePage.RefreshAsync();
                break;
        }
    }


    /// <summary>
    /// Reset visual state for all pages before switching.
    /// </summary>
    private static void PrepareContent(View view)
    {
        view.IsVisible = false;
        view.Opacity = 0;
        view.InputTransparent = true;
    }

    private View GetTabView(AppTabKind tab)
    {
        return tab switch
        {
            AppTabKind.Home => _homePage,
            AppTabKind.Notes => _notesPage,
            _ => _profilePage
        };
    }

    private Border GetTabButton(AppTabKind tab)
    {
        return tab switch
        {
            AppTabKind.Home => HomeTabButton,
            AppTabKind.Notes => NotesTabButton,
            _ => ProfileTabButton
        };
    }

    /// <summary>
    /// Update bottom tab UI state (colors, highlights).
    /// </summary>
    private void UpdateTabVisuals(AppTabKind activeTab)
    {
        ApplyHomeTabStyle(activeTab == AppTabKind.Home);
        ApplyNotesTabStyle(activeTab == AppTabKind.Notes);
        ApplyProfileTabStyle(activeTab == AppTabKind.Profile);
    }

    private void ApplyHomeTabStyle(bool isActive)
    {
        var strokeColor = isActive ? ActiveStrokeColor : InactiveColor;
        var fillColor = isActive ? ActiveFillColor : Colors.Transparent;

        HomeTabButton.BackgroundColor = isActive ? ActiveBackgroundColor : Colors.Transparent;
        HomeTabLabel.TextColor = strokeColor;
        HomeTabIcon.Stroke = new SolidColorBrush(strokeColor);
        HomeTabIcon.Fill = new SolidColorBrush(fillColor);
    }

    private void ApplyNotesTabStyle(bool isActive)
    {
        var strokeColor = isActive ? ActiveStrokeColor : InactiveColor;
        var fillColor = isActive ? ActiveFillColor : Colors.Transparent;
        var lineColor = isActive ? Colors.White : strokeColor;

        NotesTabButton.BackgroundColor = isActive ? ActiveBackgroundColor : Colors.Transparent;
        NotesTabLabel.TextColor = strokeColor;
        NotesTabFrame.Stroke = new SolidColorBrush(strokeColor);
        NotesTabFrame.Fill = new SolidColorBrush(fillColor);
        NotesTabLine1.Stroke = new SolidColorBrush(lineColor);
        NotesTabLine2.Stroke = new SolidColorBrush(lineColor);
        NotesTabLine3.Stroke = new SolidColorBrush(lineColor);
    }

    private void ApplyProfileTabStyle(bool isActive)
    {
        var strokeColor = isActive ? ActiveStrokeColor : InactiveColor;
        var fillColor = isActive ? ActiveFillColor : Colors.Transparent;

        ProfileTabButton.BackgroundColor = isActive ? ActiveBackgroundColor : Colors.Transparent;
        ProfileTabLabel.TextColor = strokeColor;
        ProfileTabHead.Stroke = new SolidColorBrush(strokeColor);
        ProfileTabHead.Fill = new SolidColorBrush(fillColor);
        ProfileTabBody.Stroke = new SolidColorBrush(strokeColor);
    }


    /// <summary>
    /// Small scale animation for tab click feedback.
    /// </summary>
    private static async Task AnimateTabButtonAsync(VisualElement tabButton)
    {
        tabButton.Scale = 1;
        await tabButton.ScaleTo(1.06, 110, Easing.CubicOut);
        await tabButton.ScaleTo(1.0, 110, Easing.CubicIn);
    }
}
