using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;

namespace MemoryWarden
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        //private WorkerThread workerThread;
        //private Thread workerThreadContainer;
        //public uint warning1Threshold { get; }
        private WarningWindow warningWindow;
        private System.Windows.Forms.Timer checkMemoryTimer;
        private List<WarningEvent> warnings;
        private uint TEMPRESETTHRESHOLD = 5;

        public MainWindow()
        {
            InitializeComponent();

            //Create tray icon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Memory Warden";
            trayIcon.Icon = Properties.Resources.icontest;
            trayIcon.MouseClick += trayIconClicked;
            trayIcon.Visible = true;
        }

        private void TypeDigitsOnly(object sender, TextCompositionEventArgs e)
        {
            char c = Convert.ToChar(e.Text);
            if (Char.IsDigit(c))
            {
                e.Handled = false;
            } else
            {
                //Do not let the child object handle this
                e.Handled = true;
            }
            base.OnPreviewTextInput(e);
        }

        private void EnsureFrequencyMakesSense(object sender, KeyboardFocusChangedEventArgs e)
        {
            if ((frequencyTextBox.Text.Length == 0) || (frequencyTextBox.Text == "0"))
            {
                frequencyTextBox.Text = "1";
            }
        }

        private void pw1Unchecked(object sender, RoutedEventArgs e)
        {
            if (pw1TextBox != null) pw1TextBox.IsEnabled = false;
        }

        private void pw1Checked(object sender, RoutedEventArgs e)
        {
            if (pw1TextBox != null) pw1TextBox.IsEnabled = true;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
        }

        private void okButtonClicked(object sender, EventArgs e)
        {
            //Hide the main window
            this.WindowState = System.Windows.WindowState.Minimized;
            this.ShowInTaskbar = false;

            //Close open windows if there are any
            if (warnings != null)
            {
                foreach (WarningEvent warning in warnings)
                {
                    if (warning.warningWindow != null)
                    {
                        warning.warningWindow.Close();
                        warning.warningWindow = null;
                    }
                }
            }

            //Prepare lits of warning thresholds
            //List<uint> passiveThresholds = new List<uint>();
            //passiveThresholds.Add(Convert.ToUInt16(pw1TextBox.Text));//TEMPORARY
            //List<uint> aggressiveThresholds = new List<uint>();
            warnings = new List<WarningEvent>();
            warnings.Add(new WarningEvent(Convert.ToUInt16(pw1TextBox.Text), WarningType.passive));
            //TEMPORARY
            //if (killThreshold > 0) warnings.Add(new WarningEvent(killThreshold, WarningType.kill));
            

            /*if (workerThread != null)
            {
                //Kill the old thread, user changed settings
                workerThread.keepRunning = false;
                workerThreadContainer.Abort();
                while (workerThreadContainer.IsAlive) Thread.Sleep(1);
                workerThreadContainer = null;
                workerThread = null;
            }
            workerThread = new WorkerThread(passiveThresholds, aggressiveThresholds, 0, 5, 1000);
            workerThread.ThresholdReached += WorkerThread_ThresholdReached;
            workerThreadContainer = new Thread(new ThreadStart(workerThread.TrackMemoryAndCreateWarnings));
            workerThreadContainer.Priority = ThreadPriority.Highest;
            workerThreadContainer.SetApartmentState(ApartmentState.STA);
            workerThreadContainer.Start();*/
            //workerThread.ThresholdReached += new EventHandler<WarningEvent>(WorkerThread_ThresholdReached);

            checkMemoryTimer = new System.Windows.Forms.Timer();
            checkMemoryTimer.Interval = 2000;
            checkMemoryTimer.Tick += CheckMemoryAndCreateWarnings;
            checkMemoryTimer.Start();
        }

        private void CheckMemoryAndCreateWarnings(object sender, EventArgs e)
        {
            double memoryUsage = SystemMemory.GetMemoryPercentUsed();

            //Check which warnings should be activated
            foreach (WarningEvent warning in warnings)
            {
                if (warning.enabled)
                {
                    //Check if the memory is too high, then activate warning if so
                    if (memoryUsage >= warning.threshold)
                    {
                        warning.enabled = false;
                        //OnThresholdReached(warning);
                        warning.warningWindow = new WarningWindow(warning.threshold, warning.type);
                        //warning.warningWindow.DataContext = this;
                        //warning.warningWindow.ShowInTaskbar = false;
                        warning.warningWindow.Show();
                    }
                }
                else
                {
                    //Check if the memory went low enough to re-enable the warning
                    if (memoryUsage <= (warning.threshold - TEMPRESETTHRESHOLD))
                    {
                        warning.enabled = true;
                    }
                }
            }
        }

        private void WorkerThread_ThresholdReached(object sender, WarningEvent warning)
        {
            warningWindow = new WarningWindow(warning.threshold, warning.type);
            //warningWindow.DataContext = this;
            warningWindow.Show();
            Console.WriteLine("Got the event, threadid=" + Thread.CurrentThread.ManagedThreadId);
        }

        private void trayIconClicked(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        private void exitButtonClicked(object sender, RoutedEventArgs e)
        {
            /*if (workerThread != null)
            {
                workerThread.keepRunning = false;
                //workerThreadContainer.Interrupt();
                workerThreadContainer.Abort();
            }*/
            this.Close();
        }
    }
}
