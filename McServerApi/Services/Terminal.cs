﻿using System.Diagnostics;

namespace LauncherGamePlugin;

public class Terminal
{
    public List<string> StdOut { get; private set; }
    public List<string> StdErr { get; private set; }
    public int ExitCode { get; private set; }
    public bool IsActive { get; private set; } = false;
    public Dictionary<string, string> Env { get; set; } = new();
    public bool Killed { get; private set; }
    public string WorkingDirectory { get; set; } = "";
    public event Action<Terminal, string> OnNewLine;
    public event Action<Terminal, string> OnNewErrLine;

    private Process proc;

    public Terminal()
    {
        OnNewLine += (terminal, s) => Log($"[{proc?.Id}] [StdOut] {s}");
        OnNewErrLine += (terminal, s) => Log($"[{proc?.Id}] [StdErr] {s}");
    }

    public async Task<bool> Exec(string fileName, string args)
    {
        IsActive = true;
        Killed = false;
        StdOut = new();
        StdErr = new();
        bool result = true;
        
        proc = new();
        proc.StartInfo = new()
        {
            Arguments = args,
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        if (!string.IsNullOrWhiteSpace(WorkingDirectory))
            proc.StartInfo.WorkingDirectory = WorkingDirectory;
        
        List<string> env = new();
        foreach (var x in Env)
        {
            proc.StartInfo.EnvironmentVariables[x.Key] = x.Value;
            proc.StartInfo.Environment[x.Key] = x.Value;
            env.Add($"{x.Key} = {x.Value}");
        }
        
        Log($"Starting terminal with command: {fileName} {args}");

        try
        {
            proc.Start();
            Thread stdOut = new Thread(CaptureStdOutOutput);
            Thread stdErr = new Thread(CaptureStdErrOutput);
            stdOut.Start();
            stdErr.Start();

            await proc.WaitForExitAsync();
            stdOut.Join();
            stdErr.Join();
        }
        catch (Exception e)
        {
            Log($"Failed to start terminal: {e.Message}");
            result = false;
        }

        if (result)
            ExitCode = proc.ExitCode;
        IsActive = false;
        Log($"Terminal exited with code {ExitCode}");
        proc.Close();
        return result;
    }
    
    private void CaptureStdErrOutput()
    {
        while (true)
        {
            string line = proc.StandardError.ReadLine();
            if (line == null)
                break;
            if (line != "")
            {
                StdErr.Add(line);
                OnNewErrLine?.Invoke(this, line);
            }
        }
    }

    private void CaptureStdOutOutput()
    {
        while (true)
        {
            string line = proc.StandardOutput.ReadLine();
            if (line == null)
                break;
            if (line != "")
            {
                StdOut.Add(line);
                OnNewLine?.Invoke(this, line);
            }
        }
    }

    public void WriteToStdIn(string message)
    {
        if (!proc.HasExited)
        {
            try
            {
                proc.StandardInput.WriteLine(message);
            }
            catch
            {
            }
        }
    }
    
    public void Kill()
    {
        if (IsActive)
        {
            Killed = true;
            proc.Kill(true);
        }
    }

    private void Log(string message) => Console.WriteLine($"[Terminal] {message}");
}