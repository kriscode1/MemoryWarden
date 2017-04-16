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
using System.ComponentModel;

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

            //Column: warning type
            DataGridComboBoxColumn warningTypeColumn = new DataGridComboBoxColumn();
            warningTypeColumn.Header = "Warning Type";
            warningTypeColumn.ItemsSource = Enum.GetNames(typeof(WarningType));
            System.Windows.Data.Binding warningTypeColumnBind = new System.Windows.Data.Binding("typeText");
            warningTypeColumnBind.Mode = BindingMode.TwoWay;
            warningTypeColumnBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            warningTypeColumn.SelectedItemBinding = warningTypeColumnBind;

            //Column: treshold value
            DataGridTextColumn thresholdValueColumn = new DataGridTextColumn();
            thresholdValueColumn.Header = "Warning triggers at this memory %";
            thresholdValueColumn.SortDirection = ListSortDirection.Ascending;//Won't actually sort by this column, just here for looks
            System.Windows.Data.Binding thresholdValueColumnBind = new System.Windows.Data.Binding("threshold");
            thresholdValueColumnBind.Mode = BindingMode.TwoWay;
            thresholdValueColumnBind.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
            thresholdValueColumnBind.ValidationRules.Add(new PercentsValidator());
            thresholdValueColumn.Binding = thresholdValueColumnBind;
            
            //Add columns in desired order
            warningsDataGrid.Columns.Clear();
            warningsDataGrid.Columns.Add(warningTypeColumn);
            warningsDataGrid.Columns.Add(thresholdValueColumn);

            //Add initial rows
            warnings = new ObservableCollection<WarningEvent>();
            warnings.Add(new WarningEvent(35, WarningType.passive));
            warnings.Add(new WarningEvent(75, WarningType.aggressive));
            warnings.Add(new WarningEvent(98, WarningType.kill));
            warningsDataGrid.ItemsSource = warnings;

            //Enable sorting on the numeric value of threshold
            warningsDataGrid.Items.SortDescriptions.Add(new SortDescription("threshold", ListSortDirection.Ascending));
            warningsDataGrid.Items.IsLiveSorting = true;
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

        private bool HasDigitsOnly(string text)
        {
            foreach (char c in text)
            {
                if ((c < '0') || (c > '9')) return false;
            }
            return true;
        }

        private void EnsureFrequencyMakesSense(object sender, KeyboardFocusChangedEventArgs e)
        {
            //Called when the user is done typing in a frequency time into the text box
            if ((frequencyTextBox.Text.Length == 0) || 
                (frequencyTextBox.Text == "0") ||
                (HasDigitsOnly(frequencyTextBox.Text) == false))
            {
                frequencyTextBox.Text = "1";
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
        }

        private void CloseAnOpenWarningWindow()
        {
            //Closes the open warning window, if found.
            if (warnings == null) return;
            if (warnings.Count == 0) return;
            foreach (WarningEvent warning in warnings)
            {
                if (warning.warningWindow != null)
                {
                    warning.warningWindow.StopRefreshTimer();
                    warning.warningWindow.Close();
                    warning.warningWindow = null;
                    break;//Should never be more than one window open now
                }
            }
        }

        private void okButtonClicked(object sender, EventArgs e)
        {
            //Perform validation checks, then start timer to monitor RAM if everything looks good.
            
            //Get the time frequency number for the timer
            //Number in box should already look good
            int frequency = Convert.ToInt32(frequencyTextBox.Text);
            if (timeFrame.SelectedIndex == 1) frequency *= 60;//Minutes to seconds
            frequency *= 1000;//Seconds to MS

            //Check that there are warnings in the warnings box
            if (warnings == null)
            {
                Console.WriteLine("Error: Warnings list is empty. Must have been set null somewhere.");
                return;
            }
            if (warnings.Count == 0)
            {
                Console.WriteLine("No warnings made.");
                return;
            }

            //Check that there is at most one kill warning
            int killWarningCount = 0;
            foreach (WarningEvent warning in warnings)
            {
                if (warning.type == WarningType.kill)
                {
                    if (++killWarningCount > 1)
                    {
                        Console.WriteLine("More than one kill warning specified.");
                        return;
                    }
                }
            }

            //Check if there are duplicate warnings for the same threshold
            HashSet<uint> duplicatesChecker = new HashSet<uint>();
            foreach (WarningEvent warning in warnings)
            {
                if (duplicatesChecker.Add(warning.threshold) == false)
                {
                    Console.WriteLine("Duplicate warning entered for threshold=" + warning.threshold);
                    return;
                }
            }

            //Hide the main window
            this.WindowState = System.Windows.WindowState.Minimized;
            this.ShowInTaskbar = false;

            //Close an open window if any, and re-enable old warnings
            CloseAnOpenWarningWindow();
            foreach (WarningEvent warning in warnings) warning.enabled = true;

            //Sort the warnings list from least to greatest
            // This is for CheckMemoryAndCreateWarnings() to only open one window at a time,
            // but avoids needing to sort every second.
            List<WarningEvent> sortHelper = new List<WarningEvent>(warnings);
            sortHelper.Sort((x, y) => x.threshold.CompareTo(y.threshold));
            warnings.Clear();//Clear and Add necessary to preserve binding settings.
            foreach (WarningEvent warning in sortHelper) warnings.Add(warning);
            
            //Start timer
            checkMemoryTimer = new System.Windows.Forms.Timer();
            checkMemoryTimer.Interval = frequency;
            checkMemoryTimer.Tick += CheckMemoryAndCreateWarnings;
            CheckMemoryAndCreateWarnings(this, null);//Call once so user doesn't wait
            checkMemoryTimer.Start();
        }

        private void CheckMemoryAndCreateWarnings(object sender, EventArgs e)
        {
            //Checks if a warning window should me made.
            //Only one warning window is permitted at a time
            double memoryUsage = SystemMemory.GetMemoryPercentUsed();

            //Check which warnings should be re-enabled, assume sorted ascending
            foreach (WarningEvent warning in warnings)
            {
                if (warning.enabled == false)
                {
                    //Check if the memory went low enough to re-enable the warning
                    if ((TEMPRESETTHRESHOLD < warning.threshold) && (memoryUsage <= (warning.threshold - TEMPRESETTHRESHOLD)))
                    {
                        warning.enabled = true;
                    }
                }
                else break;//Must have re-enabled all we can
            }

            //Now find the index of the latest warning to activate
            // This index skips duplicate, old warnings, so the user
            // will only see one popup window at a time.
            int lastWarningIndexToActivate = -1;
            for (int n = 0; n < warnings.Count; ++n)
            {
                if (memoryUsage >= warnings[n].threshold)
                {
                    if (warnings[n].enabled) lastWarningIndexToActivate = n;
                }
                else break;//Will be no warnings to activate above this one
            }

            if (lastWarningIndexToActivate != -1)
            {
                //First, disable all warnings that are going to be skipped over
                for (int n = 0; n < lastWarningIndexToActivate; ++n)
                {
                    warnings[n].enabled = false;
                }

                //Terminate any warning window still open
                CloseAnOpenWarningWindow();

                //Open the new warning window
                warnings[lastWarningIndexToActivate].enabled = false;
                warnings[lastWarningIndexToActivate].warningWindow = new WarningWindow(
                    warnings[lastWarningIndexToActivate].threshold, 
                    warnings[lastWarningIndexToActivate].type);
                warnings[lastWarningIndexToActivate].warningWindow.Show();
                Console.WriteLine("Window created.");
            }
        }
        
        private void trayIconClicked(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            if (checkMemoryTimer != null)
            {
                //Disable making warnings while user is changing settings
                checkMemoryTimer.Stop();
                checkMemoryTimer.Dispose();
            }

            //Check existing warnings for any open windows before the user changes something
            CloseAnOpenWarningWindow();
        }

        private void exitButtonClicked(object sender, RoutedEventArgs e)
        {
            CloseAnOpenWarningWindow();
            System.Windows.Application.Current.Shutdown();
            this.Close();//Yes, the application is probably still running here
        }

        private void AddWarningClicked(object sender, RoutedEventArgs e)
        {
            warnings.Add(new WarningEvent(0, WarningType.passive));
        }

        private void RemoveWarningClicked(object sender, RoutedEventArgs e)
        {
            WarningEvent selectedWarning = (WarningEvent)warningsDataGrid.SelectedItem;
            if (selectedWarning != null) warnings.Remove(selectedWarning);
        }

        private void SelectedWarningsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (warningsDataGrid.SelectedItems.Count == 0) removeWarningButton.IsEnabled = false;
            else removeWarningButton.IsEnabled = true;
        }
    }
}
