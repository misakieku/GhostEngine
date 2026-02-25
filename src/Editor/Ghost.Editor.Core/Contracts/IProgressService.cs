namespace Ghost.Editor.Core.Contracts;

public interface IProgressService
{
    void ShowProgress(string message, double progress = 0.0);
    void ShowIndeterminateProgress(string message);
    void SetProgress(double progress);
    void HideProgress();
}