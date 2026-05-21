using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace ControlLaboratorio.Agent;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length == 2 && e.Args[0] == "--guardian")
        {
            if (int.TryParse(e.Args[1], out int mainPid))
            {
                RunGuardianMode(mainPid);
                return;
            }
        }
        
        base.OnStartup(e);
        
        // Start normally
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private void RunGuardianMode(int mainPid)
    {
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        
        Task.Run(() =>
        {
            try
            {
                Process mainProcess = Process.GetProcessById(mainPid);
                mainProcess.WaitForExit();
                
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
            catch
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
            
            Environment.Exit(0);
        });
    }
}
