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
using System.Collections.ObjectModel;

namespace MemoryWarden
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        private System.Windows.Forms.Timer checkMemoryTimer;
        private ObservableCollection<WarningEvent> warnings;
        private uint TEMPRESETTHRESHOLD = 5;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            //Create tray icon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Memory Warden";
            trayIcon.Icon = Properties.Resources.icontest;
            trayIcon.MouseClick += trayIconClicked;
            trayIcon.Visible = true;

            //Programmatically build warnings table for user to modify
            warningsDataGrid.AutoGenerateColumns = false;
            warningsDataGrid.ItemsSource = null;

            //Column: warning type
            DataGridComboBoxColumn warningTypeColumn = new DataGridComboBoxColumn();
            warningTypeColumn.Header = "Warning Type";
            warningTypeColumn.ItemsSource = Enum.GetNames(typeof(WarningType));
            System.Windows.Data.Binding warningTypeColumnBind = new System.Windows.Data.Binding("typeText");
            warningTypeColumnBind.Mode = BindingMode.TwoWay;
            warningTypeColumn.SelectedItemBinding = warningTypeColumnBind;

            //Column: treshold value
            DataGridTextColumn tresholdValueColumn = new DataGridTextColumn();
            tresholdValueColumn.Header = "Warning triggers at this memory %";
            System.Windows.Data.Binding tresholdValueColumnBind = new System.Windows.Data.Binding("thresholdText");
            tresholdValueColumnBind.Mode = BindingMode.TwoWay;
            tresholdValueColumn.Binding = tresholdValueColumnBind;
            
            //Add columns in desired order
            warningsDataGrid.Columns.Clear();
            warningsDataGrid.Columns.Add(warningTypeColumn);
            warningsDataGrid.Columns.Add(tresholdValueColumn);

            //Add initial rows
            //warnings = new List<WarningEvent>();
            warnings = new ObservableCollection<WarningEvent>();
            warnings.Add(new WarningEvent(35, WarningType.aggressive));
            warnings.Add(new WarningEvent(75, WarningType.passive));
            warnings.Add(new WarningEvent(95, WarningType.kill));
            warningsDataGrid.ItemsSource = warnings;
            
        }

        private void TypeDigitsOnly(object sender, TextCompositionEventArgs e)
        {
            //Called when the user types into the a text box, to block non-digits
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
            //Called when the user is done typing in a frequency time into the text box
            if ((frequencyTextBox.Text.Length == 0) || (frequencyTextBox.Text == "0"))
            {
                frequencyTextBox.Text = "1";
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
        }

        private void okButtonClicked(object sender, EventArgs e)
        {
            //Perform validation checks, then start timer to monitor RAM if everything looks good.
            
            //Update the time frequency number for the timer
            int frequency = Convert.ToInt32(frequencyTextBox.Text);
            if (timeFrame.SelectedIndex == 1) frequency *= 60;//Minutes to seconds
            frequency *= 1000;//Seconds to MS

            //Check that there are warnings in the warnings box
            if (warnings.Count == 0)
            {
                Console.WriteLine("No warnings made.");
                return;
            }

            //TODO convert text to numbers for each warning

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
            
            //Start timer
            checkMemoryTimer = new System.Windows.Forms.Timer();
            checkMemoryTimer.Interval = frequency;
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
                        warning.warningWindow = new WarningWindow(warning.threshold, warning.type);
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
        
        private void trayIconClicked(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        private void exitButtonClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddWarningClicked(object sender, RoutedEventArgs e)
        {
            warnings.Add(new WarningEvent(90, WarningType.kill));
        }

        private void RemoveWarningClicked(object sender, RoutedEventArgs e)
        {
            WarningEvent selectedWarning = (WarningEvent)warningsDataGrid.SelectedItem;
            if (selectedWarning != null) warnings.Remove(selectedWarning);
        }
    }
}
