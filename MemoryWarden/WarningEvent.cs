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

        //Versions of the above for the user to get/modify
        public WarningType type { get { return typeHidden; } }
        public uint threshold {
            get { return thresholdHidden; }
            set {
                thresholdHidden = value;
                OnPropertyChanged("threshold");
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

        public WarningEvent(uint threshold, WarningType type)
        {
            thresholdHidden = threshold;
            typeHidden = type;
            this.enabled = true;
        }
    }
}
