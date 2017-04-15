using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemoryWarden
{
    public enum WarningType { passive, aggressive, kill };

    public class WarningEvent : INotifyPropertyChanged
    {
        public WarningType type { get; }
        public uint threshold { get; set; }
        private string thresholdTextHidden;

        //Versions of the above for the user to modify
        public string typeText { get; set; }
        public string thresholdText {
            get { return thresholdTextHidden; }
            set
            {
                //User can set text, but the uint is modified here
                if (value == null) { threshold = 0; return; }
                if (value.Length == 0) { threshold = 0; return; }
                if (HasDigitsOnly(value) == false) { threshold = 0; return; }
                threshold = Convert.ToUInt32(value);
                thresholdTextHidden = value;
                OnPropertyChanged("threshold");
                OnPropertyChanged("thresholdText");
                return;
            }
        }
        
        public bool enabled { get; set; }
        public WarningWindow warningWindow;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public WarningEvent(uint threshold, WarningType type)
        {
            this.threshold = threshold;
            this.thresholdText = threshold.ToString();
            this.type = type;
            this.typeText = type.ToString();
            this.enabled = true;
            Console.Write("writing " + threshold + " " + thresholdText);
        }

        private bool HasDigitsOnly(string text)
        {
            foreach (char c in text)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;
        }
    }
}
