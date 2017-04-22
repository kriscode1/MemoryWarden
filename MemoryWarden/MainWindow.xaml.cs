using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
//using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MemoryWarden
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.Timer checkMemoryTimer;
        private ObservableCollection<WarningEvent> warnings;
        private UserSettings userSettings;
        private Brush badCellBackground;
        private TableFormatter warningsDataGridFormatter;

        public MainWindow()
        {
            InitializeComponent();
            Icon = SharedStatics.ToImageSource(Properties.Resources.prison);
            this.DataContext = this;
            badCellBackground = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

            //Create tray icon
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = "Memory Warden";
            trayIcon.Icon = Properties.Resources.prison;
            trayIcon.MouseClick += trayIconClicked;
            trayIcon.Visible = true;
            trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;

            //// Programmatically build warnings table for user to modify
            
            //Column: warning type
            DataGridComboBoxColumn warningTypeColumn = new DataGridComboBoxColumn();
            warningTypeColumn.Header = "Warning Type";
            warningTypeColumn.ItemsSource = Enum.GetNames(typeof(WarningType));
            Binding warningTypeColumnBind = new Binding("typeText");
            warningTypeColumnBind.Mode = BindingMode.TwoWay;
            warningTypeColumnBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            warningTypeColumn.SelectedItemBinding = warningTypeColumnBind;
            warningTypeColumn.CellStyle = (Style)Resources["warningsTypeCell"];

            //Column: treshold value
            DataGridTextColumn thresholdValueColumn = new DataGridTextColumn();
            thresholdValueColumn.Header = "Warning triggers at this memory %";
            thresholdValueColumn.SortDirection = ListSortDirection.Ascending;//Won't actually sort by this column, just here for looks
            Binding thresholdValueColumnBind = new Binding("thresholdText");
            thresholdValueColumnBind.Mode = BindingMode.TwoWay;
            thresholdValueColumnBind.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
            thresholdValueColumn.Binding = thresholdValueColumnBind;
            thresholdValueColumn.CellStyle = (Style)Resources["warningsThresholdCell"];

            //Add columns in desired order
            warningsDataGrid.Columns.Clear();
            warningsDataGrid.Columns.Add(warningTypeColumn);
            warningsDataGrid.Columns.Add(thresholdValueColumn);

            //Add initial rows
            warnings = new ObservableCollection<WarningEvent>();
            warnings.Add(new WarningEvent("35", WarningType.aggressive));
            warnings.Add(new WarningEvent("75", WarningType.passive));
            warnings.Add(new WarningEvent("85", WarningType.aggressive));
            warnings.Add(new WarningEvent("92", WarningType.aggressive));
            warnings.Add(new WarningEvent("98", WarningType.kill));
            warningsDataGrid.ItemsSource = warnings;

            //Enable sorting on the numeric value of threshold
            warningsDataGrid.Items.SortDescriptions.Add(new SortDescription("threshold", ListSortDirection.Ascending));
            warningsDataGrid.Items.IsLiveSorting = true;

            //Table formatter helper, to store the settings for event handlers
            //Will likely expand in the future.
            warningsDataGridFormatter = new TableFormatter(warningsDataGrid);

            //Fill in default user settings
            userSettings = new UserSettings();
            warningResetThresholdTextBox.Text = userSettings.warningResetThreshold.ToString();
            warningWindowProcessMinTextBox.Text = userSettings.warningWindowProcessMin.ToString();
            warningWindowProcessMaxTextBox.Text = userSettings.warningWindowProcessMax.ToString();
            warningWindowProcessPercentMinTextBox.Text = userSettings.warningWindowProcessPercentMin.ToString();

        }
        private void TrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            //Bring up settings window.
            //Same as clicking on the tray icon.
            trayIconClicked(sender, null);//I don't use the args anyways.
        }

        private void TypeDigitsOnly(object sender, TextCompositionEventArgs e)
        {
            //Called when the user types into the a text box, to block non-digits
            //Used with PreviewTextInput event
            char c = Convert.ToChar(e.Text);
            if ((c < '0') || (c > '9'))
            {
                //Non-digit was typed
                //Do not let the child object handle this
                e.Handled = true;
            }
            else e.Handled = false;
            base.OnPreviewTextInput(e);
        }

        private bool HasDigitsOnly(string text)
        {
            foreach (char c in text) if ((c < '0') || (c > '9')) return false;
            return true;
        }

        private void ForceTextBoxToHavePositiveInt(object sender, KeyboardFocusChangedEventArgs e)
        {
            //Called when the user is done typing in a text box, to ensure a positive integer.
            //Example: frequency text box
            //Used with LostKeyboardFocus event
            TextBox textBox = sender as TextBox;
            if ((textBox.Text.Length == 0) ||
                (textBox.Text == "0") ||
                (HasDigitsOnly(textBox.Text) == false))
            {
                textBox.Text = "1";
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

        private DataGridCell GetCellFromCellInfo(DataGridCellInfo cellInfo)
        {
            //Helper function to convert a DataGridCellInfo object into a more useful DataGridCell.

            FrameworkElement cellContent = cellInfo.Column.GetCellContent(cellInfo.Item);
            //Because cellInfo doesn't have a Content property.
            if (cellContent != null) return (DataGridCell)cellContent.Parent;
            //Because cellInfo doesn't have a Parent property.
            return null;
        }

        private List<DataGridCell> GetCellsInColumn(DataGrid dataGrid, int columnIndex)
        {
            List<DataGridCell> ret = new List<DataGridCell>();
            dataGrid.SelectAllCells();
            foreach (DataGridCellInfo cellInfo in dataGrid.SelectedCells)
            {
                if (cellInfo.Column.DisplayIndex == columnIndex)
                {
                    DataGridCell cell = GetCellFromCellInfo(cellInfo);
                    ret.Add(cell);
                }
            }
            dataGrid.UnselectAllCells();
            return ret;
        }

        private List<DataGridCell> GetCellsInGrid(DataGrid dataGrid)
        {
            List<DataGridCell> ret = new List<DataGridCell>();
            dataGrid.SelectAllCells();
            foreach (DataGridCellInfo cellInfo in dataGrid.SelectedCells)
            {
                DataGridCell cell = GetCellFromCellInfo(cellInfo);
                ret.Add(cell);
            }
            dataGrid.UnselectAllCells();
            return ret;
        }

        private void CreateErrorTooltip(object content, UIElement placementTarget)
        {
            ToolTip errorForUser = new ToolTip();
            errorForUser.Content = content;
            errorForUser.IsOpen = true;
            errorForUser.StaysOpen = false;
            errorForUser.PlacementTarget = placementTarget;
            errorForUser.Placement = PlacementMode.Relative;
            errorForUser.PlacementRectangle = new Rect(40, 10, errorForUser.ActualWidth, errorForUser.ActualHeight);
        }

        private void okButtonClicked(object sender, EventArgs e)
        {
            //Perform validation checks, then start timer to monitor RAM if everything looks good.

            //Get the time frequency number for the timer
            int frequency;
            if (!Int32.TryParse(frequencyTextBox.Text, out frequency))
            {
                frequencyTextBox.Background = badCellBackground;
                CreateErrorTooltip("Should be a whole number unit of time.", frequencyTextBox);
                return;
            }
            if (frequency <= 0)
            {
                frequencyTextBox.Background = badCellBackground;
                CreateErrorTooltip("Time needs to be positive.", frequencyTextBox);
                return;
            }
            frequencyTextBox.ClearValue(TextBox.BackgroundProperty);//Reset previous error coloring
            if (timeFrame.SelectedIndex == 1) frequency *= 60;//Minutes to seconds
            frequency *= 1000;//Seconds to MS

            //Check that there are warnings in the warnings box
            if (warnings == null)
            {
                Console.WriteLine("Error: Warnings list is empty. Must have been set null somewhere.");
                CreateErrorTooltip("Unknown error. Please restart Memory Warden.", warningsDataGrid);
                return;
            }
            if (warnings.Count == 0)
            {
                CreateErrorTooltip("Add warnings when memory reaches high levels.", addWarningButton);
                return;
            }

            //Reset any previously set error coloring
            List<DataGridCell> allCells = GetCellsInGrid(warningsDataGrid);
            foreach (DataGridCell cell in allCells)
            {
                cell.ClearValue(DataGridCell.BackgroundProperty);
            }

            //Check each threshold column for a valid value
            List<DataGridCell> thresholdColumnCells = GetCellsInColumn(warningsDataGrid, 1);
            bool wasBadContent = false;
            foreach (DataGridCell cell in thresholdColumnCells)
            {
                //First get cell contents
                string text;
                if (cell.Content is TextBlock) text = (cell.Content as TextBlock).Text;
                else if (cell.Content is TextBox) text = (cell.Content as TextBox).Text;
                else text = "";

                //Convert number and validate
                bool badContent = false;
                int convertedNumber;
                if (text == null)
                {
                    badContent = true;
                    if (!wasBadContent) CreateErrorTooltip("Must enter a value.", cell);
                }
                else if (text.Length == 0)
                {
                    badContent = true;
                    if (!wasBadContent) CreateErrorTooltip("Must enter a value.", cell);
                }
                else if (!Int32.TryParse(text, out convertedNumber))
                {
                    badContent = true;
                    if (!wasBadContent) CreateErrorTooltip("Must enter a whole number.", cell);
                }
                else if ((convertedNumber < 0) || (convertedNumber > 100))
                {
                    badContent = true;
                    if (!wasBadContent) CreateErrorTooltip("RAM% should be between 0 to 100.", cell);
                }

                //Update UI
                if (badContent)
                {
                    cell.Background = badCellBackground;
                    wasBadContent = true;
                }
                else
                {
                    cell.ClearValue(DataGridCell.BackgroundProperty);
                }
            }
            if (wasBadContent) return;

            //Check that there is at most one kill warning
            string sharedKillWarningTooltipText =
                "Kill warning windows will automatically kill the top memory hogging\n" +
                "processes, without your input. Use this to prevent system lockup\n" +
                "for very high memory usage. Example: 95% or higher.";
            int killWarningCount = 0;
            foreach (WarningEvent warning in warnings)
            {
                if (warning.type == WarningType.kill)
                {
                    if (++killWarningCount > 1)
                    {
                        //Tell user: more than one kill warning specified
                        List<DataGridCell> cells = GetCellsInColumn(warningsDataGrid, 0);
                        bool tooltipShownOnce = false;
                        foreach (DataGridCell cell in cells)
                        {
                            WarningEvent cellWarning = cell.DataContext as WarningEvent;
                            if (cellWarning.type == WarningType.kill)
                            {
                                cell.Background = badCellBackground;
                                if (!tooltipShownOnce)
                                {
                                    string errorText =
                                        "Cannot have more than one kill warning type.\n\n" +
                                        sharedKillWarningTooltipText;
                                    CreateErrorTooltip(errorText, cell);
                                    tooltipShownOnce = true;
                                }
                            }
                        }
                        return;
                    }
                }
            }

            //If there is a kill warning, check if it is the last warning
            // The warnings are not yet sorted, so cannot check using indices.
            // Finding the largest threshold instead.
            if (killWarningCount == 1)
            {
                //First get index of the last warning (largest threshold)
                int largestThresholdIndex = 0;
                uint largestThreshold = warnings[0].threshold;
                for (int n = 1; n < warnings.Count; ++n)
                {
                    if (warnings[n].threshold > largestThreshold) largestThresholdIndex = n;
                }

                //Then, check if that warning is the kill warning
                if (warnings[largestThresholdIndex].type != WarningType.kill)
                {
                    //Loop through each cell until the kill cell is found
                    List<DataGridCell> cells = GetCellsInColumn(warningsDataGrid, 0);
                    foreach (DataGridCell cell in cells)
                    {
                        WarningEvent cellWarning = cell.DataContext as WarningEvent;
                        if (cellWarning.type == WarningType.kill)
                        {
                            //Found the bad kill warning. Tell user and stop the search.
                            cell.Background = badCellBackground;
                            string errorText =
                                "Kill warning must be last.\n\n" +
                                sharedKillWarningTooltipText;
                            CreateErrorTooltip(errorText, cell);
                            return;
                        }
                    }
                }
            }

            //Check if there are duplicate warnings for the same threshold
            HashSet<uint> duplicatesChecker = new HashSet<uint>();
            foreach (WarningEvent warning in warnings)
            {
                if (duplicatesChecker.Add(warning.threshold) == false)
                {
                    //Loop through the cells to highlight duplicates
                    bool tooltipShownOnce = false;
                    List<DataGridCell> cells = GetCellsInColumn(warningsDataGrid, 1);
                    foreach (DataGridCell cell in cells)
                    {
                        WarningEvent cellWarning = cell.DataContext as WarningEvent;
                        if (cellWarning.threshold == warning.threshold)
                        {
                            cell.Background = badCellBackground;
                            if (!tooltipShownOnce)
                            {
                                CreateErrorTooltip("Cannot have duplicate warnings for the same memory threshold.", cell);
                                tooltipShownOnce = true;
                            }
                        }
                    }
                    return;
                }
            }

            //Check and set the user's advanced settings
            //To prevent any conflicts with an existing window, set the numbers after all validation
            UserSettings pendingSettings = new UserSettings();
            pendingSettings.warningResetThreshold = (uint)Convert.ToInt32(warningResetThresholdTextBox.Text);
            if (pendingSettings.warningResetThreshold > 100)
            {
                expander.IsExpanded = true;
                warningResetThresholdTextBox.Background = badCellBackground;
                CreateErrorTooltip("Warning reset threshold cannot be over 100%.", warningResetThresholdTextBox);
                return;
            }
            else warningResetThresholdTextBox.ClearValue(TextBox.BackgroundProperty);
            pendingSettings.warningWindowProcessMin = (uint)Convert.ToInt32(warningWindowProcessMinTextBox.Text);
            pendingSettings.warningWindowProcessMax = (uint)Convert.ToInt32(warningWindowProcessMaxTextBox.Text);
            if (pendingSettings.warningWindowProcessMax < pendingSettings.warningWindowProcessMin)
            {
                expander.IsExpanded = true;
                warningWindowProcessMinTextBox.Background = badCellBackground;
                warningWindowProcessMaxTextBox.Background = badCellBackground;
                CreateErrorTooltip(
                    "Minimum number of processes to show must be less than\n" +
                    "or equal to the maximum number of processes to show.",
                    warningWindowProcessMaxTextBox);
                return;
            }
            else
            {
                warningWindowProcessMinTextBox.ClearValue(TextBox.BackgroundProperty);
                warningWindowProcessMaxTextBox.ClearValue(TextBox.BackgroundProperty);
            }
            pendingSettings.warningWindowProcessPercentMin = (uint)Convert.ToInt32(warningWindowProcessPercentMinTextBox.Text);
            if (pendingSettings.warningWindowProcessPercentMin > 100)
            {
                expander.IsExpanded = true;
                warningWindowProcessPercentMinTextBox.Background = badCellBackground;
                CreateErrorTooltip("Minimum cumulative process responsibility cannot be over 100%.", warningWindowProcessPercentMinTextBox);
                return;
            }
            else warningWindowProcessPercentMinTextBox.ClearValue(TextBox.BackgroundProperty);
            userSettings = pendingSettings;

            //Hide the main window
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;

            //Show the tray icon balloon tip
            trayIcon.ShowBalloonTip(
                3000,
                "Memory Warden Running",
                "Settings saved. Ready to give warnings.\nClick this icon to stop and open settings again.",
                System.Windows.Forms.ToolTipIcon.None);

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
                    if ((userSettings.warningResetThreshold < warning.threshold) &&
                        (memoryUsage <= (warning.threshold - userSettings.warningResetThreshold)))
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
                    warnings[lastWarningIndexToActivate].type,
                    userSettings);
                warnings[lastWarningIndexToActivate].warningWindow.Show();
            }
        }

        private void trayIconClicked(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = WindowState.Normal;
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

            //Notify the user that monitoring is paused
            trayIcon.ShowBalloonTip(
                3000,
                "Memory Warden Paused",
                "Memory warnings are paused.",
                System.Windows.Forms.ToolTipIcon.None);
        }

        private void exitButtonClicked(object sender, RoutedEventArgs e)
        {
            CloseAnOpenWarningWindow();
            Application.Current.Shutdown();
            this.Close();//Yes, the application is probably still running here
        }

        private void AddWarningClicked(object sender, RoutedEventArgs e)
        {
            warnings.Add(new WarningEvent("0", WarningType.passive));
        }

        private void RemoveWarningClicked(object sender, RoutedEventArgs e)
        {
            List<WarningEvent> selectedWarningsCopy = new List<WarningEvent>();
            foreach (WarningEvent warning in warningsDataGrid.SelectedItems)
            {
                selectedWarningsCopy.Add(warning);
            }
            foreach (WarningEvent warning in selectedWarningsCopy)
            {
                warnings.Remove(warning);
            }
        }

        private void SelectedWarningsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (warningsDataGrid.SelectedItems.Count == 0) removeWarningButton.IsEnabled = false;
            else removeWarningButton.IsEnabled = true;
        }

        private void HighlightTextboxContentsOnFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        private void Row_MouseEnter(object sender, MouseEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) warningsDataGridFormatter.MouseEnterRow(row);
        }
        private void Row_MouseLeave(object sender, MouseEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) warningsDataGridFormatter.MouseLeaveRow(row);
        }
        private void Row_Selected(object sender, RoutedEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) warningsDataGridFormatter.RowSelected(row);
        }
        private void Row_Unselected(object sender, RoutedEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) warningsDataGridFormatter.RowUnselected(row);
        }
        private void Cell_Selected(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null) warningsDataGridFormatter.CellSelected(cell);
        }
        private void Cell_Unselected(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null) warningsDataGridFormatter.CellUnselected(cell);
        }
    }
}
