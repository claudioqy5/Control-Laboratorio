using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Threading;

namespace ControlLaboratorio.Agent
{
    public partial class SessionWindow : Window
    {
        private readonly int _sesionId;
        private readonly MainWindow _lockWindow;
        private readonly HttpClient _httpClient = new HttpClient();
        private double _remainingSeconds;
        private DispatcherTimer _countdownTimer;
        private DispatcherTimer _pollingTimer;

        public SessionWindow(int sesionId, string userName, double remainingSeconds, MainWindow lockWindow)
        {
            InitializeComponent();
            _sesionId = sesionId;
            _remainingSeconds = remainingSeconds;
            _lockWindow = lockWindow;
            lblUser.Text = userName;

            // Posicionar arriba a la derecha de la pantalla principal
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 30;
            this.Top = 30;

            // Capturar apagado/reinicio del sistema para cerrar sesión
            Application.Current.SessionEnding += Current_SessionEnding;

            // Evitar fugas de memoria limpiando el evento al cerrar la ventana normalmente
            this.Closed += (s, e) =>
            {
                try
                {
                    Application.Current.SessionEnding -= Current_SessionEnding;
                }
                catch { }
            };

            SetupTimers();
        }

        private void SetupTimers()
        {
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromSeconds(5); // Poll fast for remote locking
            _pollingTimer.Tick += PollingTimer_Tick;
            _pollingTimer.Start();

            UpdateTimerDisplay();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
            }
            
            UpdateTimerDisplay();
            
            if (_remainingSeconds <= 0)
            {
                _countdownTimer.Stop();
                _pollingTimer.Stop();
                ForceLogout();
            }
        }

        private async void PollingTimer_Tick(object sender, EventArgs e)
        {
            if (_sesionId == 0) return; // SuperAdmin mode, no API syncing

            try
            {
                var response = await _httpClient.GetFromJsonAsync<SessionStatusResponse>($"{MainWindow.ApiUrl}/session-status/{_sesionId}");
                if (response != null)
                {
                    if (response.IsFinished)
                    {
                        _countdownTimer.Stop();
                        _pollingTimer.Stop();
                        ForceLogout();
                    }
                    else
                    {
                        // Solo sincronizar si la diferencia es mayor a 5 segundos para evitar saltos
                        if (Math.Abs(response.RemainingSeconds - _remainingSeconds) > 5)
                        {
                            _remainingSeconds = response.RemainingSeconds;
                        }
                    }
                }
            }
            catch { /* Ignore network errors */ }
        }

        private void UpdateTimerDisplay()
        {
            var remaining = TimeSpan.FromSeconds(_remainingSeconds);
            if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;
            lblTimer.Text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            
            if (remaining.TotalMinutes <= 15)
                lblTimer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            else
                lblTimer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 204, 21)); // #FACC15
        }

        private async void ForceLogout()
        {
            if (_sesionId != 0)
            {
                try
                {
                    await _httpClient.PostAsJsonAsync($"{MainWindow.ApiUrl}/logout", new { SesionId = _sesionId });
                }
                catch { }
            }
            
            _lockWindow.ClearFields();
            _lockWindow.Show();
            this.Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer?.Stop();
            _pollingTimer?.Stop();
            ForceLogout();
        }

        private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (_sesionId != 0)
            {
                try
                {
                    // Llamada síncrona súper rápida para registrar el cierre de sesión en base de datos antes del shutdown
                    var response = _httpClient.PostAsJsonAsync($"{MainWindow.ApiUrl}/logout", new { SesionId = _sesionId }).GetAwaiter().GetResult();
                }
                catch { }
            }
        }
    }

    public class SessionStatusResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("horaLimite")]
        public DateTime? HoraLimite { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("isFinished")]
        public bool IsFinished { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("remainingSeconds")]
        public double RemainingSeconds { get; set; }
    }
}
