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
        private const string ApiUrl = "https://localhost:7215/api/auth"; // Update with actual URL
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
            this.Closing += (s, e) => UnhookWindowsHookEx(_hookID);

            // Iniciar timer para desbloqueo remoto
            _remoteUnlockTimer = new DispatcherTimer();
            _remoteUnlockTimer.Interval = TimeSpan.FromSeconds(3);
            _remoteUnlockTimer.Tick += RemoteUnlockTimer_Tick;
            _remoteUnlockTimer.Start();
        }

        private async void RemoteUnlockTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible) return;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<RemoteUnlockResponse>($"{ApiUrl}/check-remote-unlock/{Environment.MachineName}");
                if (response != null && response.Unlock)
                {
                    var sessionBar = new SessionWindow(0, "SUPERADMIN (Remoto)", DateTime.Now.AddHours(3), this);
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
            // Si el código es el de emergencia, desbloqueamos sin internet.
            if (txtCodigo.Text == "ADMIN_MASTER_99")
            {
                MessageBox.Show("ACCESO DE EMERGENCIA ACTIVADO", "SuperAdministrador");
                this.Close(); // O Hide() dependiendo de si quieres cerrar el app
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
                    
                    // Abrir la barra de sesión y ocultar el bloqueo
                    var sessionBar = new SessionWindow(_currentSesionId, result.Alumno.Nombres, result.HoraLimite, this);
                    sessionBar.Show();
                    this.Hide();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    lblError.Text = "Error: Credenciales inválidas o equipo bloqueado.";
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
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);

                // Bloquear Alt+Tab, Alt+F4, Win Key
                bool alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
                
                if (key == Key.System && alt && (key == Key.Tab || key == Key.F4)) return (IntPtr)1;
                if (key == Key.LWin || key == Key.RWin) return (IntPtr)1;
                if (alt && key == Key.Tab) return (IntPtr)1;
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
    }
}