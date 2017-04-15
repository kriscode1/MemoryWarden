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
        public double RamPercentRaw { get; }
        private int PIDRaw;
        private string NameRaw;

        public string RamPercent { get { return string.Format("{0:F2}", RamPercentRaw); } }
        public string PID { get { return PIDRaw.ToString(); } }
        public string Name { get { return NameRaw; } }

        public ProcessRow(Process process, long totalMemoryBytes)
        {
            PIDRaw = process.Id;
            NameRaw = process.ProcessName;
            RamPercentRaw = (double) process.WorkingSet64 / totalMemoryBytes * 100;
        }

        //Method implementations for sorting

        public int CompareTo(ProcessRow other)
        {
            if (other == null) return 1;
            if (this.RamPercentRaw != other.RamPercentRaw)
            {
                double difference = this.RamPercentRaw - other.RamPercentRaw;
                return Convert.ToInt32(100000000 * difference);//Arbitrary precision
            }
            //Ram percents are the same, sort by PID now because that is unique
            if (this.PIDRaw != other.PIDRaw) return (this.PIDRaw - other.PIDRaw);
            return 0;
        }
    }
    
    public partial class WarningWindow : Window
    {
        public List<ProcessRow> processTable;
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

            //Trim results to top 20%
            double cumulativeMemorySum = 0.0;
            for (int n = 0; n < processTable.Count; ++n)
            {
                cumulativeMemorySum += processTable[n].RamPercentRaw;
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

            //Sort by first column descending
            /*memoryHogs.ItemsSource = processTable;
            memoryHogs.Columns.First().SortDirection = System.ComponentModel.ListSortDirection.Descending;
            memoryHogs.Items.SortDescriptions.Clear();
            memoryHogs.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(memoryHogs.Columns.First().SortMemberPath, System.ComponentModel.ListSortDirection.Descending));
            memoryHogs.Items.Refresh();*/

            //Set the data
            memoryHogs.ItemsSource = processTable;
        }
    }
}
