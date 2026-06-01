using System;
using System.Diagnostics;
class Program {
    static void Main() {
        Process current = Process.GetCurrentProcess();
        foreach (Process process in Process.GetProcessesByName(current.ProcessName))
        {
            if (process.Id != current.Id)
            {
                try {
                    if (process.HasExited) continue;
                } catch { continue; }
                Console.WriteLine("Found active");
                Environment.Exit(0);
            }
        }
        Console.WriteLine("Started");
    }
}
