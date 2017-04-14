using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryWarden
{
    public enum WarningType { passive, aggressive, kill };

    public class WarningEvent
    {
        public WarningType type { get; }
        public uint threshold { get; }
        public bool enabled { get; set; }
        public WarningWindow warningWindow;

        public WarningEvent(uint threshold, WarningType type)
        {
            this.threshold = threshold;
            enabled = true;
            this.type = type;
        }
    }
}
