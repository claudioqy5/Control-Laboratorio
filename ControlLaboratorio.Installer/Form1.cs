using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlLaboratorio.Installer
{
    public partial class Form1 : Form
    {
        private Label lblTitle;
        private Label lblDescription;
        private TextBox txtPath;
        private Button btnInstall;
        private ProgressBar progressBar;
        private Label lblStatus;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Instalador - Control Laboratorio Agent";
            this.Size = new Size(500, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            lblTitle = new Label { Text = "BVE - Instalador del Agent", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            lblDescription = new Label { Text = "Este asistente instalará el sistema de control en este equipo.\nSe configurará para iniciarse automáticamente.", Font = new Font("Segoe UI", 10), Location = new Point(25, 60), Size = new Size(400, 40) };
            
            var lblPath = new Label { Text = "Ruta de instalación:", Font = new Font("Segoe UI", 9), Location = new Point(25, 120), AutoSize = true };
            txtPath = new TextBox { Text = @"C:\BVE_Agent", Location = new Point(25, 140), Width = 430, Font = new Font("Segoe UI", 9) };
            
            progressBar = new ProgressBar { Location = new Point(25, 190), Width = 430, Height = 20, Visible = false };
            lblStatus = new Label { Text = "Listo para instalar.", Location = new Point(25, 215), AutoSize = true, Font = new Font("Segoe UI", 8), ForeColor = Color.Gray };

            btnInstall = new Button { Text = "Instalar", Location = new Point(355, 260), Size = new Size(100, 30), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnInstall.Click += BtnInstall_Click;

            var lblCredit = new Label { Text = "Desarrollado por ClaudiiioQY", Font = new Font("Segoe UI", 7, FontStyle.Italic), ForeColor = Color.LightGray, Location = new Point(25, 280), AutoSize = true };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblDescription);
            this.Controls.Add(lblPath);
            this.Controls.Add(txtPath);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnInstall);
            this.Controls.Add(lblCredit);
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            btnInstall.Enabled = false;
            txtPath.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            string targetDir = txtPath.Text;
            string targetExe = Path.Combine(targetDir, "ControlLaboratorio.Agent.exe");

            try
            {
                lblStatus.Text = "Creando directorio...";
                await Task.Delay(500);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                lblStatus.Text = "Copiando archivos del sistema...";
                await Task.Run(() => ExtractResource("ControlLaboratorio.Installer.Resources.ControlLaboratorio.Agent.exe", targetExe));

                lblStatus.Text = "Configurando inicio automático (Registro de Windows)...";
                await Task.Run(() => CreateStartupShortcut(targetExe));

                lblStatus.Text = "Iniciando el sistema...";
                await Task.Delay(500);
                
                Process.Start(new ProcessStartInfo { FileName = targetExe, UseShellExecute = true });

                MessageBox.Show("Instalación completada con éxito. El sistema ya está funcionando.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la instalación:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
                txtPath.Enabled = true;
                progressBar.Style = ProgressBarStyle.Blocks;
                lblStatus.Text = "Instalación fallida.";
            }
        }

        private void ExtractResource(string resourceName, string outPath)
        {
            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) throw new Exception($"El recurso interno {resourceName} no fue encontrado.");
            
            if (File.Exists(outPath))
            {
                try
                {
                    foreach (var process in Process.GetProcessesByName("ControlLaboratorio.Agent"))
                    {
                        process.Kill();
                    }
                    Thread.Sleep(1000); // Dar tiempo al OS para liberar el archivo
                }
                catch { }
            }

            using FileStream fileStream = new FileStream(outPath, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fileStream);
        }

        private void CreateStartupShortcut(string exePath)
        {
            // Método más seguro y rápido usando el Registro de Windows en lugar de accesos directos de PowerShell
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!)
            {
                key.SetValue("BVE_ControlLaboratorioAgent", $"\"{exePath}\"");
            }
        }
    }
}
