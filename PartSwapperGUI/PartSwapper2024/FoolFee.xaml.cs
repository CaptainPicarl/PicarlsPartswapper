using ScottPlot.TickGenerators.TimeUnits;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PartSwapperGUI.PartSwapper2024
{
    /// <summary>
    /// Interaction logic for FoolFee.xaml
    /// </summary>
    public partial class FoolFee : Window
    {
        internal System.Timers.Timer countdownTimer = new System.Timers.Timer();
        private int countdownValue = 10;
        private Mutex Mutex = new Mutex();
        public FoolFee()
        {
            InitializeComponent();

            this.Closing += OnCloseAttempt;
            this.Loaded += FoolFee_Loaded;

        }

        private void FoolFee_Loaded(object sender, RoutedEventArgs e)
        {
            countdownTimer.Interval = 1000;
            countdownTimer.Elapsed += OnCountdownUpdate;
            countdownTimer.Enabled = true;
            countdownTimer.AutoReset = true;

            this.Activate();
        }

        private void OnCountdownUpdate(Object source, ElapsedEventArgs e)
        {
            if (Mutex.WaitOne())
            {
                if (countdownValue == 0)
                {
                    Mutex.ReleaseMutex();

                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        this.Close();
                    }));
                }
                else
                {
                    this.countdownValue -= 1;

                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        this.CountdownTimerTextBlock.Text = $"Picarl's Partswapper 2024 will open {(countdownValue.ToString().Equals("0") ? "now!" : ("in " + countdownValue.ToString() + " seconds..."))}";
                    }));

                }
                Mutex.ReleaseMutex();
            }
            else
            {
                Trace.WriteLine("Unable to get Mutex!");
            }
        }

        private void OnCloseAttempt(object? sender, System.ComponentModel.CancelEventArgs e) {

            if (countdownValue == 0)
            {
                e.Cancel = false;
                countdownTimer.Stop();
                countdownTimer.Close();
                countdownTimer.Dispose();

                this.Mutex.Close();
                this.Mutex.Dispose();
            }
            else
            {
                e.Cancel = true;
            }

        }
    }
}
