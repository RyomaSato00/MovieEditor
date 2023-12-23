namespace MovieEditor.Models;

internal interface IAnyProcess
{
    public event Action<int, int>? OnUpdateProgress;
    public void Cancel();
}