using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ControlLaboratorio.Agent
{
    public partial class MainWindow : Window
    {
#if DEBUG
        public const string ApiUrl = "http://localhost:5087/api/auth"; // Desarrollo local
#else
        public const string ApiUrl = "https://bvefamurp.helifyferdigital.cloud/api/auth"; // Producción
#endif
        private readonly HttpClient _httpClient = new HttpClient();
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private int _currentSesionId = 0;
        private DispatcherTimer _remoteUnlockTimer;
        private Process _guardianProcess;

        public MainWindow()
        {
            InitializeComponent();
            lblMachineName.Text = $"Equipo: {Environment.MachineName}";
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            this.Closing += (s, e) => { if (this.Visibility == Visibility.Visible) e.Cancel = true; else UnhookWindowsHookEx(_hookID); };

            // Mantener la ventana al frente y a pantalla completa para bloquear correctamente
            this.Topmost = true;
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Deactivated += (s, e) => { if (this.Visibility == Visibility.Visible) this.Activate(); };

            // Forzar foco y bloqueo inmediato de pantalla
            this.Loaded += (s, e) => { this.Activate(); this.Focus(); };

            // Iniciar timer para desbloqueo remoto
            _remoteUnlockTimer = new DispatcherTimer();
            _remoteUnlockTimer.Interval = TimeSpan.FromSeconds(3);
            _remoteUnlockTimer.Tick += RemoteUnlockTimer_Tick;
            _remoteUnlockTimer.Start();
            this.Activated += (s, e) => txtCodigo.Focus();

            this.Loaded += MainWindow_Loaded;

            // Lanzar proceso guardián con nombre diferente (WinSystemHost.exe en Temp)
            try
            {
                int myPid = Process.GetCurrentProcess().Id;
                string exePath = Process.GetCurrentProcess().MainModule!.FileName;
                // Copiar el exe a Temp con un nombre neutro de sistema
                string guardianPath = Path.Combine(Path.GetTempPath(), "WinSystemHost.exe");
                File.Copy(exePath, guardianPath, overwrite: true);
                // Lanzar la COPIA pasándole también la ruta real del agente para que pueda reiniciarlo
                _guardianProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = guardianPath,
                    Arguments = $"--guardian {myPid} \"{exePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch { }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Registrar el equipo en el backend al arrancar (para que aparezca en el panel admin)
            await RegisterEquipmentAsync();
            // Verificar si hay una sesión activa pendiente de restaurar
            await CheckActiveSessionAsync();
        }

        private async Task RegisterEquipmentAsync()
        {
            // Reintentar hasta 5 veces con espera incremental para garantizar el registro
            // incluso si el backend tarda en responder al arrancar la PC
            int maxRetries = 5;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{ApiUrl}/register-equipment", new
                    {
                        NombreRed = Environment.MachineName
                    });

                    if (response.IsSuccessStatusCode)
                        return; // Registro exitoso, no necesitamos más reintentos
                }
                catch { /* Sin conexión todavía, reintentamos */ }

                if (attempt < maxRetries)
                {
                    // Espera incremental: 2s, 4s, 8s, 16s entre intentos
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
        }

        private async Task CheckActiveSessionAsync()
        {
            try
            {
                // Reusamos el HttpClient local apuntando a la ruta que terminamos en AuthController
                string baseUrl = ApiUrl.Replace("/api/auth", "");
                var response = await _httpClient.GetFromJsonAsync<ActiveSessionResponse>($"{baseUrl}/api/auth/active-session/{Environment.MachineName}");
                if (response != null && response.HasActiveSession)
                {
                    _currentSesionId = response.SesionId;
                    string fullName = (response.Alumno?.Nombres + " " + response.Alumno?.Apellidos).Trim();
                    if (string.IsNullOrEmpty(fullName)) fullName = "Estudiante (Restaurado)";
                    
                    var sessionBar = new SessionWindow(_currentSesionId, fullName, response.RemainingSeconds, this);
                    sessionBar.Show();
                    this.Hide();
                }
            }
            catch { /* Silencioso en caso de error de red al iniciar */ }
        }

        private async void RemoteUnlockTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible) return;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<RemoteUnlockResponse>($"{ApiUrl}/check-remote-unlock/{Environment.MachineName}");
                if (response != null)
                {
                    if (response.Shutdown)
                    {
                        Process.Start(new ProcessStartInfo("shutdown", "/s /t 5") { CreateNoWindow = true, UseShellExecute = false });
                        return;
                    }
                    if (response.Unlock)
                    {
                        // Abrir la sesión como Administrador (5 horas = 18000 segundos)
                        var sessionBar = new SessionWindow(response.SesionId, "ADMINISTRADOR (Remoto)", 18000, this);
                        sessionBar.Show();
                        this.Hide();
                    }
                }
            }
            catch { }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Visibility = Visibility.Collapsed;
            
            // LOGICA DE EMERGENCIA (SUPERADMINISTRADOR)
            if (txtCodigo.Text == "ADMIN_MASTER_99" && txtDNI.Password == "masterfamurp")
            {
                // Intentar registrar el equipo silenciosamente por si hay conexión
                try
                {
                    await _httpClient.PostAsJsonAsync($"{ApiUrl}/register-equipment", new
                    {
                        NombreRed = Environment.MachineName
                    });
                }
                catch { /* Falla silenciosamente si no hay internet */ }

                try { _guardianProcess?.Kill(); } catch { }
                MessageBox.Show("ACCESO DE EMERGENCIA ACTIVADO", "SuperAdministrador");
                Application.Current.Shutdown(); // Cerrar todo el sistema
                return;
            }

            btnLogin.IsEnabled = false;

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{ApiUrl}/login", new
                {
                    CodigoUniversitario = txtCodigo.Text,
                    DNI = txtDNI.Password,
                    NombreRed = Environment.MachineName
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    _currentSesionId = result.SesionId;
                    
                    // Limpiar y ocultar el bloqueo
                    ClearFields();
                    var sessionBar = new SessionWindow(_currentSesionId, result.Alumno.Nombres, result.RemainingSeconds, this);
                    sessionBar.Show();
                    this.Hide();
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    string errorMessage = "Error: Credenciales inválidas o equipo bloqueado.";
                    
                    try 
                    {
                        using (var doc = System.Text.Json.JsonDocument.Parse(errorJson))
                        {
                            if (doc.RootElement.TryGetProperty("message", out var msgElement))
                            {
                                errorMessage = msgElement.GetString();
                            }
                        }
                    }
                    catch { /* Fallback */ }
                    
                    lblError.Text = errorMessage;
                    lblError.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Error de conexión con el servidor.";
                lblError.Visibility = Visibility.Visible;
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        public void ClearFields()
        {
            txtCodigo.Text = "";
            txtDNI.Password = "";
            lblError.Visibility = Visibility.Collapsed;
            txtCodigo.Focus();
        }

        #region Win32 Keyboard Hook logic

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                // Bloqueos críticos:
                // 91/92 = Tecla Windows, 115 = F4 (Alt+F4), 9 = Tab (Alt+Tab), 27 = Esc (Ctrl+Esc)
                bool isAlt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
                bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

                if (vkCode == 91 || vkCode == 92) return (IntPtr)1; // Teclas Windows
                if (isAlt && vkCode == 115) return (IntPtr)1;      // Alt + F4
                if (isAlt && vkCode == 9) return (IntPtr)1;        // Alt + Tab
                if (isCtrl && vkCode == 27) return (IntPtr)1;      // Ctrl + Esc
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }

    public class LoginResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("sesionId")]
        public int SesionId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("horaLimite")]
        public DateTime HoraLimite { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("remainingSeconds")]
        public double RemainingSeconds { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("alumno")]
        public AlumnoData Alumno { get; set; }
    }

    public class AlumnoData
    {
        [System.Text.Json.Serialization.JsonPropertyName("nombres")]
        public string Nombres { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("apellidos")]
        public string Apellidos { get; set; }
    }

    public class RemoteUnlockResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("unlock")]
        public bool Unlock { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("shutdown")]
        public bool Shutdown { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("sesionId")]
        public int SesionId { get; set; }
    }

    public class ActiveSessionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("hasActiveSession")]
        public bool HasActiveSession { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("sesionId")]
        public int SesionId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("horaLimite")]
        public DateTime HoraLimite { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("remainingSeconds")]
        public double RemainingSeconds { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("alumno")]
        public AlumnoData Alumno { get; set; }
    }
}