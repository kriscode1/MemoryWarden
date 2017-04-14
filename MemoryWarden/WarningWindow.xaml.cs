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

    public class ProcessRow
    {
        public double ramPercent { get; }
        public int PID { get; }
        public string Name { get; }

        public ProcessRow(Process process, long totalMemoryBytes)
        {
            PID = process.Id;
            Name = process.ProcessName;
            ramPercent = (double) process.WorkingSet64 / totalMemoryBytes * 100;
        }
    }
    
    public partial class WarningWindow : Window
    {
        public List<ProcessRow> processTable;

        public WarningWindow(uint memoryExceededThreshold, WarningType warningType)
        {
            InitializeComponent();
            this.DataContext = this;

            memoryValue.Content = memoryExceededThreshold;

            Process[] processes = Process.GetProcesses();

            //Calculate total memory used
            long totalProcessesMemory = 0;
            for (int n = 0; n < processes.Length; ++n)
            {
                totalProcessesMemory += processes[n].WorkingSet64;
            }

            processTable = new List<ProcessRow>();
            for (int n = 0; n < processes.Length; ++n)
            {
                processTable.Add(new ProcessRow(processes[n], totalProcessesMemory));
            }

            //Set the data
            memoryHogs.ItemsSource = processTable;

            //Sort by first column descending
            memoryHogs.Columns.First().SortDirection = System.ComponentModel.ListSortDirection.Descending;
            memoryHogs.Items.SortDescriptions.Clear();
            memoryHogs.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(memoryHogs.Columns.First().SortMemberPath, System.ComponentModel.ListSortDirection.Descending));
            memoryHogs.Items.Refresh();
        }
    }
}
