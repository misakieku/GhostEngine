namespace Ghost.Editor.Contracts;

internal interface INavigationAware
{
    public void OnNavigatedTo(object? parameter);
    public void OnNavigatedFrom();
}