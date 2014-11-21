using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmOperatorGroup
    {
        public string Name { get; set; }
        public int Position { get; set; }

        public SCADAlarmOperatorGroup()
        {
            Name = string.Empty;
            Position = 0;
        }
    }
}
