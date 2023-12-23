namespace MovieEditor.Models;

internal interface IAnyProcess
{
    public event Action<int>? OnStartProcess;

    public event Action<int>? OnUpdateProgress;
    public void Cancel();
}