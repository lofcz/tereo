namespace TeReoLocalizer.Shared.Code;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Threading.Tasks;

public class LiveSyncMSBuildWorkspace : IDisposable
{
    public readonly MSBuildWorkspace Workspace;
    public Solution CurrentSolution;

    public LiveSyncMSBuildWorkspace()
    {
        Workspace = MSBuildWorkspace.Create();
        Workspace.WorkspaceChanged += OnWorkspaceChanged;
    }

    public async Task OpenSolutionAsync(string solutionPath)
    {
        CurrentSolution = await Workspace.OpenSolutionAsync(solutionPath);
    }
    
    public bool SetCurrentSolution(Solution newSolution)
    {
        bool success = Workspace.TryApplyChanges(newSolution);
        
        if (success)
        {
            CurrentSolution = newSolution;
        }
        
        return success;
    }

    private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        Console.WriteLine("aktualizována otevřená solution");
        CurrentSolution = e.NewSolution;
    }
    
    public void Dispose()
    {
        Workspace.WorkspaceChanged -= OnWorkspaceChanged;
        Workspace.Dispose();
        GC.SuppressFinalize(this);
    }
}