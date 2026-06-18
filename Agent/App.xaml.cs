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
        try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Starting up... PID={Process.GetCurrentProcess().Id}, Args={string.Join(",", e.Args)}\n"); } catch { }
        // Forzar arranque automático en Windows (Registro)
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    string currentExe = Environment.ProcessPath!;
                    key.SetValue("ControlLaboratorioAgent", $"\"{currentExe}\"");
                }
            }
        }
        catch { }

        if (e.Args.Length >= 2 && e.Args[0] == "--guardian")
        {
            if (int.TryParse(e.Args[1], out int mainPid))
            {
                // La ruta real del agente es el tercer argumento (si existe)
                string agentPath = e.Args.Length >= 3 ? e.Args[2] : Environment.ProcessPath!;
                RunGuardianMode(mainPid, agentPath);
                return;
            }
        }

        // Verificar si fuimos reiniciados por el guardián
        bool isRestartedByGuardian = e.Args.Length > 0 && e.Args[0] == "--restarted-by-guardian";
        if (isRestartedByGuardian)
        {
            try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Restarted by Guardian. Aggressively cleaning up old zombie instances.\n"); } catch { }
        }

        // Control de instancia única (Basado en procesos para evitar problemas de handles con el Guardián)
        Process current = Process.GetCurrentProcess();
        try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Checking processes for {current.ProcessName}\n"); } catch { }
        foreach (Process process in Process.GetProcessesByName(current.ProcessName))
        {
            if (process.Id != current.Id)
            {
                try 
                {
                    if (!process.HasExited)
                    {
                        if (isRestartedByGuardian)
                        {
                            // Si fuimos reiniciados por el guardián, asumimos que somos el legítimo sucesor
                            // y que este otro proceso es un zombie. Lo matamos sin piedad.
                            try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Killing zombie process {process.Id}.\n"); } catch { }
                            try { process.Kill(); } catch { }
                        }
                        else
                        {
                            // Darle un tiempo de gracia a la otra instancia por si está en proceso de cerrarse/morir (ej. Task Manager kill)
                            process.WaitForExit(2000);
                            if (!process.HasExited)
                            {
                                try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Found active process {process.Id}. Exiting.\n"); } catch { }
                                // Ya hay una instancia ejecutándose firmemente. Salir de inmediato.
                                Environment.Exit(0);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (isRestartedByGuardian)
                    {
                        try { process.Kill(); } catch { }
                    }
                    else
                    {
                        try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Exception checking process {process.Id}: {ex.Message}. Exiting.\n"); } catch { }
                        // Si no podemos acceder, asumimos que sigue viva
                        Environment.Exit(0);
                        return;
                    }
                }
            }
        }

        try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Proceeding to show MainWindow.\n"); } catch { }

        base.OnStartup(e);

        // Inicio normal
        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            mainWindow.Activate(); // Forzar foco inmediato para bloquear la pantalla
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(@"C:\BVE_Agent\startup_log.txt", $"{DateTime.Now}: Exception in MainWindow: {ex}\n"); } catch { }
        }
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

                // Protección máxima: Matar al Administrador de Tareas si el alumno intenta abrirlo
                try
                {
                    foreach (var tm in Process.GetProcessesByName("Taskmgr"))
                    {
                        try { tm.Kill(); } catch { }
                    }
                }
                catch { }

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
                            Arguments = "--restarted-by-guardian",
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
