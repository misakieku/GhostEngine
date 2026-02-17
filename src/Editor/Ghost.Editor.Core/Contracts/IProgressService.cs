namespace Ghost.Editor.Core.Contracts;

public interface IProgressService
{
    public void ShowProgress(string message, double progress = 0.0);
    public void ShowIndeterminateProgress(string message);
    public void SetProgress(double progress);
    public void HideProgress();
}