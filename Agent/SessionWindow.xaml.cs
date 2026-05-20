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
        private DateTime _horaLimite;
        private DispatcherTimer _countdownTimer;
        private DispatcherTimer _pollingTimer;

        public SessionWindow(int sesionId, string userName, DateTime horaLimite, MainWindow lockWindow)
        {
            InitializeComponent();
            _sesionId = sesionId;
            _horaLimite = horaLimite;
            _lockWindow = lockWindow;
            lblUser.Text = userName;

            // Posicionar arriba a la derecha de la pantalla principal
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 30;
            this.Top = 30;

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
            UpdateTimerDisplay();
            
            if (DateTime.Now >= _horaLimite)
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
                    else if (response.HoraLimite.HasValue)
                    {
                        _horaLimite = response.HoraLimite.Value;
                    }
                }
            }
            catch { /* Ignore network errors */ }
        }

        private void UpdateTimerDisplay()
        {
            var remaining = _horaLimite - DateTime.Now;
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
    }

    public class SessionStatusResponse
    {
        public DateTime? HoraLimite { get; set; }
        public bool IsFinished { get; set; }
    }
}
