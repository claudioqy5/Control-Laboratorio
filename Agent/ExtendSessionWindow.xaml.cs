using System;
using System.Windows;

namespace ControlLaboratorio.Agent
{
    public partial class ExtendSessionWindow : Window
    {
        /// <summary>
        /// true si el alumno aceptó extender, false si rechazó o cerró.
        /// </summary>
        public bool Accepted { get; private set; } = false;

        public ExtendSessionWindow()
        {
            InitializeComponent();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            this.Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            this.Close();
        }
    }
}
