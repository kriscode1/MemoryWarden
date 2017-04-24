using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;

namespace MemoryWarden
{
    /// <summary>
    /// Interaction logic for WarningWindow.xaml
    /// </summary>
    /// 

    public class ProcessRow : INotifyPropertyChanged
    {
        //Contains the bound data for the table of memory hog processes.

        private double ramPercentHidden;
        public double ramPercent {
            get { return ramPercentHidden; }
            set {
                ramPercentHidden = value;
                OnPropertyChanged("ramPercent");
                OnPropertyChanged("ramPercentText");
            }
        }
        public int PID { get; }
        private string name;

        public string ramPercentText { get { return string.Format("{0:F2}", ramPercent); } }
        public string PIDText { get { return PID.ToString(); } }
        public string nameText { get { return name; } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ProcessRow(Process process, long totalMemoryBytes)
        {
            PID = process.Id;
            name = process.ProcessName;
            ramPercent = (double) process.WorkingSet64 / totalMemoryBytes * 100;
        }
    }
    
    public partial class WarningWindow : Window
    {
        private ObservableCollection<ProcessRow> processTable;
        private Process[] processes;
        private List<Process> processesSorted;
        private long totalProcessesMemory;
        private System.Windows.Forms.Timer refreshTimer;
        private double systemMemoryPercent;
        private UserSettings userSettings;
        private TableFormatter memoryHogsFormatter;
        private bool killMode;
        private uint memoryExceededThreshold;

        public WarningWindow(uint memoryExceededThreshold, WarningType warningType, UserSettings userSettings)
        {
            InitializeComponent();
            this.userSettings = userSettings;
            killMode = (warningType == WarningType.kill);
            this.memoryExceededThreshold = memoryExceededThreshold;
            Icon = SharedStatics.ToImageSource(Properties.Resources.bars_white);
            
            //Formatter helper for the table settings, will likely expand in the future.
            memoryHogsFormatter = new TableFormatter(memoryHogs);

            //Set the GUI labels
            memoryValue.Content = memoryExceededThreshold;
            SetSystemMemoryPercentAndLabel();

            //Build the table with data
            RefreshProcessTable(userSettings.warningWindowProcessMin, userSettings.warningWindowProcessMax, userSettings.warningWindowProcessPercentMin);
            memoryHogs.ItemsSource = processTable;

            //Enable live sorting in the window too, if data updates but I don't resort
            memoryHogs.Items.SortDescriptions.Add(new SortDescription("ramPercent", ListSortDirection.Descending));
            memoryHogs.Items.IsLiveSorting = true;
            
            //Be passive/aggressive/kill
            if (warningType == WarningType.passive)
            {
                SharedStatics.FlashWindow(this, SharedStatics.FLASHW_ALL | SharedStatics.FLASHW_TIMERNOFG, 1, 1);
                
            }
            else if (warningType == WarningType.aggressive)
            {
                this.Show();
                this.Activate();
                this.Topmost = true;
                SharedStatics.FlashWindow(this, SharedStatics.FLASHW_ALL, flashCount:3);
            }
            else if (warningType == WarningType.kill)
            {
                //Will do the kill tasks in the next timer cycle so this constructor can finish
                this.Show();
                this.Activate();
                this.Topmost = true;
                SharedStatics.FlashWindow(this, SharedStatics.FLASHW_ALL, flashRate:200, flashCount: 6);
            }

            //Start the update timer
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Interval = 1000;
            refreshTimer.Start();
        }

        public void StopRefreshTimer()
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            //Updates the window once per second so it looks responsive.

            RefreshProcessTable(userSettings.warningWindowProcessMin, userSettings.warningWindowProcessMax, userSettings.warningWindowProcessPercentMin); ;
            SetSystemMemoryPercentAndLabel();
            if (killMode)
            {
                refreshTimer.Stop();
                KillMemoryHogs();
                refreshTimer.Start();
                killMode = false;
            }
        }

        private void KillMemoryHogs()
        {
            String tempOutput = "";
            StreamWriter log = new StreamWriter("log.txt", true);
            log.AutoFlush = true;
            int attemptedKillCount = 0;
            while (systemMemoryPercent > memoryExceededThreshold)
            {
                tempOutput = String.Format("{0} System memory {1:F2}% > {2:F2}%\n", DateTime.Now.ToString(), systemMemoryPercent, memoryExceededThreshold);
                log.Write(tempOutput);
                Process hog = processesSorted[attemptedKillCount];
                log.Write("Killing process:\n");
                log.Write("\tName:\t" + hog.ProcessName + "\n");
                log.Write("\tPID:\t" + hog.Id + "\n");
                log.Write("Result:\t");
                Idk processKilled = Idk.IDK;
                try { hog.Kill(); }
                catch
                {
                    processKilled = CheckProcessIsKilled(hog);
                    if (processKilled != Idk.TRUE)
                    {
                        //Maybe the process is killed,
                        log.Write("Kill failed.\n\n");
                        continue;// but move on from this error anyways.
                    }
                }
                finally
                {
                    ++attemptedKillCount;
                }
                if (processKilled == Idk.IDK) processKilled = CheckProcessIsKilled(hog);

                //Documentation states Kill() is asynchronous,
                //and I do not want to block until the process is killed.
                //Kill() did not error, so wait a little to see if it worked
                int waitCount = 0;
                while ((processKilled != Idk.TRUE) && (waitCount < 10))
                {
                    System.Threading.Thread.Sleep(100);//0.1 seconds
                    ++waitCount;
                    processKilled = CheckProcessIsKilled(hog);
                }

                //If process was killed, wait 1 second before killing the next.
                if (processKilled == Idk.TRUE)
                {
                    log.Write("Kill succeeded.\n\n");
                    System.Threading.Thread.Sleep(1000);
                }
                else if (processKilled == Idk.FALSE)
                {
                    log.Write("Kill failed.\n\n");
                }
                else
                {
                    log.Write("Kill status uncertain.\n\n");
                }

                //Checking RAM again, whether the killed process was exited or not
                systemMemoryPercent = SystemMemory.GetMemoryPercentUsed();
            }
            tempOutput = String.Format("{0} System memory {1:F2}% is below the warning level again.\n", DateTime.Now, systemMemoryPercent);
            log.Write(tempOutput);
            log.Close();
        }

        private Idk CheckProcessIsKilled(Process p)
        {
            bool hasExited;
            try { hasExited = p.HasExited; }
            catch (InvalidOperationException)
            {
                //There is no process associated with the object.
                return Idk.TRUE;
            }
            catch (Win32Exception)
            {
                //The exit code for the process could not be retrieved.
                return Idk.IDK;
            }
            catch (NotSupportedException)
            {
                //You are trying to access the HasExited property for a process
                //that is running on a remote computer.This property is available
                //only for processes that are running on the local computer.
                return Idk.FALSE;
            }
            if (hasExited) return Idk.TRUE;
            return Idk.FALSE;
        }

        private void RefreshProcessLists()
        {
            //Get a list of processes, outputs to processes and processesSorted.
            processes = Process.GetProcesses();

            //Build the sorted list
            processesSorted = new List<Process>(processes);
            //Negate the comparison to sort descending
            processesSorted.Sort((x, y) => -x.WorkingSet64.CompareTo(y.WorkingSet64));
        }

        private void CalculateTotalProcessMemory()
        {
            //Calculate total memory used, outputs to totalProcessesMemory.

            // Add process memory as a total instead of using the system total,
            // used for consistent percentages.
            totalProcessesMemory = 0;
            for (int n = 0; n < processes.Length; ++n)
            {
                totalProcessesMemory += processes[n].WorkingSet64;
            }
        }

        private void RefreshProcessTable(uint minProcessCount, uint maxProcessCount, uint minMemoryPercent)
        {
            //Do all the work of rebuilding the processTable, attempting efficiency.
            
            RefreshProcessLists();
            CalculateTotalProcessMemory();
            ObservableCollection<ProcessRow> processTableTemp = new ObservableCollection<ProcessRow>();
            double cumulativeMemorySum = 0.0;
            for (int n = 0; n < processes.Length; ++n)
            {
                //Don't care after these limits are reached
                if (((cumulativeMemorySum > minMemoryPercent) && (n >= minProcessCount)) || 
                    (n >= maxProcessCount)) break;
                
                ProcessRow temp = new ProcessRow(processes[n], totalProcessesMemory);
                cumulativeMemorySum += temp.ramPercent;
                processTableTemp.Add(temp);
            }

            if (processTable == null)
            {
                //First run, simply assign
                processTable = processTableTemp;
            }
            else
            {
                //Now merge the temp table with the original
                //Performance should be fine because these lists are kept small
                foreach (ProcessRow tempRow in processTableTemp)
                {
                    bool matchFound = false;
                    foreach (ProcessRow originalRow in processTable)
                    {
                        if (originalRow.PID == tempRow.PID)
                        {
                            originalRow.ramPercent = tempRow.ramPercent;
                            matchFound = true;
                            break;
                        }
                    }
                    if (!matchFound)
                    {
                        //Add the tempRow instead
                        processTable.Add(tempRow);
                    }
                }

                //Cleanup removed processes
                ObservableCollection<ProcessRow> processesToRemove = new ObservableCollection<ProcessRow>();
                foreach (ProcessRow originalRow in processTable)
                {
                    bool matchFound = false;
                    foreach (ProcessRow tempRow in processTableTemp)
                    {
                        if (originalRow.PID == tempRow.PID)
                        {
                            matchFound = true;
                            break;
                        }
                    }
                    if (!matchFound)
                    {
                        //Old process not in new table
                        processesToRemove.Add(originalRow);
                    }
                }
                foreach (ProcessRow processToRemove in processesToRemove)
                {
                    processTable.Remove(processToRemove);
                }
            }
        }

        private void SetSystemMemoryPercentAndLabel()
        {
            //Gets the system memory percent and performs GUI tasks.

            systemMemoryPercent = SystemMemory.GetMemoryPercentUsed();
            systemMemoryLabel.Content = string.Format("{0:F2}", systemMemoryPercent);

            //Calcualte colors
            Brush systemBasedBrush = SharedStatics.CalculateMemoryBrush(systemMemoryPercent);

            //Apply brush wherever
            systemMemoryLabel.Background = systemBasedBrush;
            warningLabel.Background = systemBasedBrush;
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Ask the user if the process should be terminated in a popup window.

            refreshTimer.Stop();
            DataGridRow row = sender as DataGridRow;
            ProcessRow processRow = row.Item as ProcessRow;
            string messageText =
                "Should Memory Warden kill this process?\n" +
                "Name:\t" + processRow.nameText + "\n" +
                "PID:\t" + processRow.PIDText + "\n" +
                "Memory Usage: " + processRow.ramPercentText + "%";
            MessageBoxResult queryUser = MessageBox.Show(this, messageText, "Kill Process", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No);
            if (queryUser == MessageBoxResult.Yes)
            {
                int processIndex = processesSorted.FindIndex(x => x.Id == processRow.PID);
                if (processIndex == -1)
                {
                    messageText = "Cannot kill process. \nBad internal index.";
                    DisplayErrorMessage(messageText, "Kill Process Failed");
                    Console.WriteLine(messageText);
                    return;
                }
                try { processesSorted[processIndex].Kill(); }
                catch (Win32Exception exception)
                {
                    messageText = "This process could not be killed.\n" + exception.Message;
                    DisplayErrorMessage(messageText, "Kill Process Failed");
                }
                catch (NotSupportedException exception)
                {
                    messageText = "This is a remote process and cannot be killed.\n" + exception.Message;
                    DisplayErrorMessage(messageText, "Kill Process Failed");
                }
                catch (InvalidOperationException exception)
                {
                    messageText = "This process does not exist anymore.\n" + exception.Message;
                    DisplayErrorMessage(messageText, "Kill Process Failed");
                }
            }
            refreshTimer.Start();
        }

        private void DisplayErrorMessage(string message, string title)
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }

        private void Row_MouseEnter(object sender, MouseEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) memoryHogsFormatter.MouseEnterRow(row);
        }
        private void Row_MouseLeave(object sender, MouseEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) memoryHogsFormatter.MouseLeaveRow(row);
        }
        private void Row_Selected(object sender, RoutedEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) memoryHogsFormatter.RowSelected(row);
        }
        private void Row_Unselected(object sender, RoutedEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null) memoryHogsFormatter.RowUnselected(row);
        }
        private void Cell_Selected(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null) memoryHogsFormatter.CellSelected(cell);
        }
        private void Cell_Unselected(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null) memoryHogsFormatter.CellUnselected(cell);
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Console.WriteLine("closing");
            StopRefreshTimer();

        }
    }
}
