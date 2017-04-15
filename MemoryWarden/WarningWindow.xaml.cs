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

namespace MemoryWarden
{
    /// <summary>
    /// Interaction logic for WarningWindow.xaml
    /// </summary>
    /// 

    public class ProcessRow : IComparable<ProcessRow>
    {
        public double ramPercent { get; }
        private int PID;
        private string name;

        public string ramPercentText { get { return string.Format("{0:F2}", ramPercent); } }
        public string PIDText { get { return PID.ToString(); } }
        public string nameText { get { return name; } }

        public ProcessRow(Process process, long totalMemoryBytes)
        {
            PID = process.Id;
            name = process.ProcessName;
            ramPercent = (double) process.WorkingSet64 / totalMemoryBytes * 100;
        }

        //Method implementations for sorting

        public int CompareTo(ProcessRow other)
        {
            if (other == null) return 1;
            if (this.ramPercent != other.ramPercent)
            {
                double difference = this.ramPercent - other.ramPercent;
                return Convert.ToInt32(100000000 * difference);//Arbitrary precision
            }
            //Ram percents are the same, sort by PIDText now because that is unique
            if (this.PID != other.PID) return (this.PID - other.PID);
            return 0;
        }
    }
    
    public partial class WarningWindow : Window
    {
        public List<ProcessRow> processTable;//Todo make updatable
        Process[] processes;

        public WarningWindow(uint memoryExceededThreshold, WarningType warningType)
        {
            InitializeComponent();
            this.DataContext = this;//For binding

            //Set the GUI label
            memoryValue.Content = memoryExceededThreshold;
            
            //Get a list of processes
            processes = Process.GetProcesses();

            //Calculate total memory used
            // Add process memory as a total instead of using the system total,
            // for consistent percentages.
            long totalProcessesMemory = 0;
            for (int n = 0; n < processes.Length; ++n)
            {
                totalProcessesMemory += processes[n].WorkingSet64;
            }

            //Build and sort the table
            processTable = new List<ProcessRow>();
            for (int n = 0; n < processes.Length; ++n)
            {
                processTable.Add(new ProcessRow(processes[n], totalProcessesMemory));
            }

            //Sort the table
            processTable.Sort();
            processTable.Reverse();

            //Trim results to top 20%, or top 8 processes
            double cumulativeMemorySum = 0.0;
            for (int n = 0; n < processTable.Count; ++n)
            {
                cumulativeMemorySum += processTable[n].ramPercent;
                if (cumulativeMemorySum >= 20)
                {
                    ++n;
                    if (n < 8) n = 8;
                    if (n < processTable.Count) {
                        processTable.RemoveRange(n, processTable.Count - n);
                    }
                    break;
                }
            }

            //Set the data
            memoryHogs.ItemsSource = processTable;
        }
    }
}
