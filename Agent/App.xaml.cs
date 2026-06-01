using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ControlLaboratorio.Agent;

public partial class App : Application
{
    public const string AgentVersion = "1.0.5";

    protected override void OnStartup(StartupEventArgs e)
    {
        // Forzar arranque automático en Windows (Registro)
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    string currentExe = Process.GetCurrentProcess().MainModule!.FileName;
                    key.SetValue("ControlLaboratorioAgent", $"\"{currentExe}\"");
                }
            }
        }
        catch { }

        // Modo guardián: --guardian <PID> <RutaDelAgente>
        if (e.Args.Length >= 2 && e.Args[0] == "--guardian")
        {
            if (int.TryParse(e.Args[1], out int mainPid))
            {
                // La ruta real del agente es el tercer argumento (si existe)
                string agentPath = e.Args.Length >= 3 ? e.Args[2] : Process.GetCurrentProcess().MainModule!.FileName;
                RunGuardianMode(mainPid, agentPath);
                return;
            }
        }

        // Control de instancia única (Basado en procesos para evitar problemas de handles con el Guardián)
        Process current = Process.GetCurrentProcess();
        foreach (Process process in Process.GetProcessesByName(current.ProcessName))
        {
            if (process.Id != current.Id)
            {
                // Ya hay una instancia ejecutándose. Salir de inmediato.
                Environment.Exit(0);
                return;
            }
        }

        base.OnStartup(e);

        // Inicio normal
        var mainWindow = new MainWindow();
        mainWindow.Show();
        mainWindow.Activate(); // Forzar foco inmediato para bloquear la pantalla
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }

    private void RunGuardianMode(int mainPid, string agentExePath)
    {
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Task.Run(() =>
        {
            while (true)
            {
                System.Threading.Thread.Sleep(2000); // revisar cada 2 segundos

                bool mainAlive = false;
                try
                {
                    var p = Process.GetProcessById(mainPid);
                    mainAlive = !p.HasExited;
                }
                catch { mainAlive = false; }

                if (!mainAlive)
                {
                    // El agente fue cerrado → reiniciarlo usando su ruta real
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = agentExePath,
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                    }
                    catch { /* Si falla, el guardián termina */ }

                    Environment.Exit(0);
                }
            }
        });
    }
}
