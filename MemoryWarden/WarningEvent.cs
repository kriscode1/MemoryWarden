using System;
using System.ComponentModel;

namespace MemoryWarden
{
    public enum WarningType { passive, aggressive, kill };

    public class WarningEvent : INotifyPropertyChanged
    {
        //Info to describe when a warning should be given to the user.

        private WarningType typeHidden;
        private uint thresholdHidden;
        private string thresholdTextHidden;

        //Versions of the above for the user to get/modify
        public WarningType type { get { return typeHidden; } }
        public uint threshold {
            get { return thresholdHidden; }
            /*set {
                thresholdHidden = value;
                OnPropertyChanged("threshold");
            }*/
        }
        public string thresholdText
        {
            get { return thresholdTextHidden; }
            set
            {
                int result;
                if (Int32.TryParse(value, out result))
                {
                    if (result >= 0) thresholdHidden = (uint)result;
                    OnPropertyChanged("threshold");
                }
                thresholdTextHidden = value;
                OnPropertyChanged("thresholdText");
            }
        }
        public string typeText {
            get { return typeHidden.ToString(); }
            set {
                WarningType result;
                if (Enum.TryParse<WarningType>(value, out result))
                {
                    typeHidden = result;
                    OnPropertyChanged("typeText");
                    OnPropertyChanged("type");
                }
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

        public WarningEvent(string threshold, WarningType type)
        {
            this.thresholdText = threshold;
            typeHidden = type;
            this.enabled = true;
        }
    }
}
