using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        private double ramPercentHidden;
        public double ramPercent {
            get { return ramPercentHidden; }
            set {
                ramPercentHidden = value;
                OnPropertyChanged("ramPercent");
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

        public WarningWindow(uint memoryExceededThreshold, WarningType warningType)
        {
            InitializeComponent();
            this.DataContext = this;//For binding

            //Set the GUI labels
            memoryValue.Content = memoryExceededThreshold;
            SetSystemMemoryPercentAndLabel();

            RefreshProcessTable();
            
            //Enable live sorting in the window too, if data updates but I don't resort
            memoryHogs.Items.SortDescriptions.Add(new SortDescription("ramPercent", ListSortDirection.Descending));
            memoryHogs.Items.IsLiveSorting = true;
            
            //Set the data
            memoryHogs.ItemsSource = processTable;

            //Be aggressive
            if (warningType == WarningType.aggressive)
            {
                this.Show();
                this.Activate();
            }

            //Start the update timer
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Interval = 1000;
            refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshProcessTable();
            //memoryHogs.ItemsSource = null;
            //memoryHogs.ItemsSource = processTable;
            memoryHogs.Items.Refresh();
            SetSystemMemoryPercentAndLabel();
        }

        private void RefreshProcessLists()
        {
            //Get a list of processes
            processes = Process.GetProcesses();

            //Build the sorted list
            processesSorted = new List<Process>(processes);
            //Negate the comparison to sort descending
            processesSorted.Sort((x, y) => -x.WorkingSet64.CompareTo(y.WorkingSet64));

        }

        private void CalculateTotalProcessMemory()
        {
            //Calculate total memory used
            // Add process memory as a total instead of using the system total,
            // used for consistent percentages.
            totalProcessesMemory = 0;
            for (int n = 0; n < processes.Length; ++n)
            {
                totalProcessesMemory += processes[n].WorkingSet64;
            }
        }

        private void RefreshProcessTable(double memoryLimit = 20.0, int minimumProcessCount = 8)
        {
            RefreshProcessLists();
            CalculateTotalProcessMemory();
            ObservableCollection<ProcessRow> processTableTemp = new ObservableCollection<ProcessRow>();
            double cumulativeMemorySum = 0.0;
            for (int n = 0; n < processes.Length; ++n)
            {
                //Don't care after these limits are reached
                if ((cumulativeMemorySum > memoryLimit) && (n > minimumProcessCount)) break;
                
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
                        processTable.Remove(originalRow);
                    }
                }
            }
        }

        private void SetSystemMemoryPercentAndLabel()
        {
            systemMemoryPercent = SystemMemory.GetMemoryPercentUsed();
            systemMemoryLabel.Content = string.Format("{0:F2}", systemMemoryPercent);

            //Calcualte colors
            double memoryGoodRatio;
            //Used for calculating the green/red color
            //60% or less is good, 90% or more is bad
            if (systemMemoryPercent <= 60) memoryGoodRatio = 1;
            else if (systemMemoryPercent >= 90) memoryGoodRatio = 0;
            else
            {
                memoryGoodRatio = (90 - systemMemoryPercent) / 30;
            }
            double green = memoryGoodRatio * 0xFF;
            double red = (1 - memoryGoodRatio) * 0xFF;
            Brush systemBasedBrush = new SolidColorBrush(Color.FromArgb(0xFF, (byte)red, (byte)green, 0));

            //Apply brush wherever
            systemMemoryLabel.Background = systemBasedBrush;
            warningLabel.Background = systemBasedBrush;
        }
    }
}
