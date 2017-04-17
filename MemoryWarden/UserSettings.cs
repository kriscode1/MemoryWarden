using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryWarden
{
    public class UserSettings
    {
        //Class to manage user settings and transfer between windows.

        public uint warningResetThreshold { get; set; }
        public uint warningWindowProcessMin { get; set; }
        public uint warningWindowProcessMax { get; set; }
        public uint warningWindowProcessPercentMin { get; set; }

        public UserSettings()
        {
            //Default user settings
            warningResetThreshold = 5;
            warningWindowProcessMin = 8;
            warningWindowProcessMax = 50;
            warningWindowProcessPercentMin = 20;
        }
    }
}
