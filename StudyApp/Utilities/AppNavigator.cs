namespace StudyApp.Utilities;

public static class AppNavigator
{
    public static Task PushAsync(Page page)
    {
        var navigation = GetActivePage()?.Navigation;
        return navigation is null ? Task.CompletedTask : navigation.PushAsync(page);
    }

    public static Task PushModalAsync(Page page)
    {
        var navigation = GetActivePage()?.Navigation;
        return navigation is null ? Task.CompletedTask : navigation.PushModalAsync(page);
    }

    public static Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var page = GetActivePage();
        return page is null ? Task.CompletedTask : page.DisplayAlert(title, message, cancel);
    }

    public static Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        var page = GetActivePage();
        return page is null ? Task.FromResult(false) : page.DisplayAlert(title, message, accept, cancel);
    }

    public static Page? GetActivePage()
    {
        return GetActivePage(Application.Current?.MainPage);
    }

    private static Page? GetActivePage(Page? page)
    {
        if (page is null)
        {
            return null;
        }

        var modalPage = page.Navigation.ModalStack.LastOrDefault();
        if (modalPage is not null && modalPage != page)
        {
            return GetActivePage(modalPage);
        }

        return page switch
        {
            NavigationPage navigationPage => GetActivePage(navigationPage.CurrentPage),
            FlyoutPage flyoutPage => GetActivePage(flyoutPage.Detail),
            TabbedPage tabbedPage => GetActivePage(tabbedPage.CurrentPage),
            Shell shell when shell.CurrentPage is not null => GetActivePage(shell.CurrentPage),
            _ => page
        };
    }
}
