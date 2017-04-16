using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryWarden
{
    public class UserSettings// : INotifyPropertyChanged
    {
        /*private uint warningResetThresholdHidden;
        public uint warningResetThreshold {
            get { return warningResetThresholdHidden; }
            set {
                warningResetThresholdHidden = value;
                OnPropertyChanged("warningResetThreshold");
            }
        }*/
        public uint warningResetThreshold { get; set; }
        public uint warningWindowProcessMin { get; set; }
        public uint warningWindowProcessMax { get; set; }
        public uint warningWindowProcessPercentMin { get; set; }

        public UserSettings()
        {
            warningResetThreshold = 5;
            warningWindowProcessMin = 8;
            warningWindowProcessMax = 50;
            warningWindowProcessPercentMin = 20;
        }

        /*public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }*/
    }
}
