using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program {
    static void Main(string[] args) {
        if (args.Length >= 2 && args[0] == "--guardian") {
            int pid = int.Parse(args[1]);
            string exe = args[2];
            Console.WriteLine($"[Guardian] Watching PID {pid}, exe {exe}");
            while(true) {
                Thread.Sleep(500);
                bool alive = false;
                try { alive = !Process.GetProcessById(pid).HasExited; } catch { }
                if (!alive) {
                    Console.WriteLine("[Guardian] Main died! Restarting...");
                    Process.Start(new ProcessStartInfo { FileName = exe, Arguments = "--restarted", UseShellExecute = true });
                    Environment.Exit(0);
                }
            }
        }
        
        Console.WriteLine($"[Main] Started. PID: {Process.GetCurrentProcess().Id}");
        string guardianPath = Path.Combine(Path.GetTempPath(), "GuardianTest.exe");
        try {
            foreach (var p in Process.GetProcessesByName("GuardianTest")) { p.Kill(); }
        } catch { }
        for (int i=0; i<5; i++) {
            try { File.Copy(Environment.ProcessPath, guardianPath, true); break; } catch { Thread.Sleep(500); }
        }
        Process.Start(new ProcessStartInfo { FileName = guardianPath, Arguments = $"--guardian {Process.GetCurrentProcess().Id} \"{Environment.ProcessPath}\"", UseShellExecute = false });
        Console.WriteLine("[Main] Guardian launched. Sleeping for 20 seconds...");
        Thread.Sleep(20000);
    }
}
