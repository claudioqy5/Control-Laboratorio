using System;
using System.Diagnostics;
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
        private const string ApiUrl = "https://bvefamurp.helifyferdigital.cloud/api/auth"; // Producción
        private readonly HttpClient _httpClient = new HttpClient();
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private int _currentSesionId = 0;
        private DispatcherTimer _remoteUnlockTimer;

        public MainWindow()
        {
            InitializeComponent();
            lblMachineName.Text = $"Equipo: {Environment.MachineName}";
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            this.Closing += (s, e) => { if (this.Visibility == Visibility.Visible) e.Cancel = true; else UnhookWindowsHookEx(_hookID); };

            // Iniciar timer para desbloqueo remoto
            _remoteUnlockTimer = new DispatcherTimer();
            _remoteUnlockTimer.Interval = TimeSpan.FromSeconds(3);
            _remoteUnlockTimer.Tick += RemoteUnlockTimer_Tick;
            _remoteUnlockTimer.Start();
            this.Activated += (s, e) => txtCodigo.Focus();
        }

        private async void RemoteUnlockTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible) return;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<RemoteUnlockResponse>($"{ApiUrl}/check-remote-unlock/{Environment.MachineName}");
                if (response != null && response.Unlock)
                {
                    // Abrir la sesión como Administrador
                    var sessionBar = new SessionWindow(response.SesionId, "ADMINISTRADOR (Remoto)", DateTime.Now.AddHours(5), this);
                    sessionBar.Show();
                    this.Hide();
                }
            }
            catch { }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Visibility = Visibility.Collapsed;
            
            // LOGICA DE EMERGENCIA (SUPERADMINISTRADOR)
            if (txtCodigo.Text == "ADMIN_MASTER_99")
            {
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
                    var sessionBar = new SessionWindow(_currentSesionId, result.Alumno.Nombres, result.HoraLimite, this);
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
        public int SesionId { get; set; }
        public DateTime HoraLimite { get; set; }
        public AlumnoData Alumno { get; set; }
    }

    public class AlumnoData
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
    }

    public class RemoteUnlockResponse
    {
        public bool Unlock { get; set; }
        public int SesionId { get; set; }
    }
}