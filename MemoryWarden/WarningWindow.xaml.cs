using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

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

        public WarningWindow(uint memoryExceededThreshold, WarningType warningType, UserSettings userSettings)
        {
            InitializeComponent();
            this.userSettings = userSettings;
            Icon = SharedStatics.ToImageSource(Properties.Resources.prison);
            //this.DataContext = this;//For binding
            
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
            double memoryGoodRatio;
            // Used for calculating the green/red color
            // 60% or less is good, 90% or more is bad
            if (systemMemoryPercent <= 60) memoryGoodRatio = 1;
            else if (systemMemoryPercent >= 90) memoryGoodRatio = 0;
            else memoryGoodRatio = (90 - systemMemoryPercent) / 30;

            double green = memoryGoodRatio * 0xFF;
            double red = (1 - memoryGoodRatio) * 0xFF;
            Brush systemBasedBrush = new SolidColorBrush(Color.FromArgb(0xFF, (byte)red, (byte)green, 0));

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

        /*private void Row_MouseEnter(object sender, MouseEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            //row.Background = new SolidColorBrush(Colors.LightCyan);
            //row.BorderBrush = new SolidColorBrush(Colors.Violet);//.LightBlue);
            //row.BorderThickness = new Thickness(0, 1, 0, 1);
            Console.WriteLine("Mouse enter this row");
        }
        private void Row_MouseLeave(object sender, MouseEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            //row.Background = new SolidColorBrush(Colors.White);
            //row.BorderBrush = new SolidColorBrush(Colors.Transparent);
            //row.BorderThickness = new Thickness(0, 1, 0, 1);
            Console.WriteLine("Mouse leave this row");
        }

        private void Cell_MouseEnter(object sender, MouseEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            
            //<DataGridCell> allCells = GetCellsInGrid(memoryHogs);
            Console.WriteLine("Mouse enter this cell, parent: " + cell.Parent.GetType().ToString());
        }
        private void Cell_MouseLeave(object sender, MouseEventArgs e)
        {
            Console.WriteLine("Mouse leave this cell");
        }*/


        private void DisplayErrorMessage(string message, string title)
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
    }
}
