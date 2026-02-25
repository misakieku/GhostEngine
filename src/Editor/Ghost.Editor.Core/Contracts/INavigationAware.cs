namespace Ghost.Editor.Core.Contracts;

public interface INavigationAware
{
    void OnNavigatedTo(object? parameter);
    void OnNavigatedFrom();
}