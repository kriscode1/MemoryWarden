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

    public class WarningEvent
    {
        public WarningType type { get; }
        public uint threshold { get; }

        //Versions of the above for the user to modify
        public string typeText { get; set; }
        public string thresholdText { get; set; }

        public bool enabled { get; set; }
        public WarningWindow warningWindow;
        
        public WarningEvent(uint threshold, WarningType type)
        {
            this.threshold = threshold;
            thresholdText = threshold.ToString();
            this.type = type;
            typeText = type.ToString();
            enabled = true;
        }
    }
}
