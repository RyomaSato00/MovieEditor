namespace MovieEditor.Models;

internal interface IAnyProcess
{
    public int FileCount { get; }
    public event Action<int>? OnUpdateProgress;
    public void Cancel();
}