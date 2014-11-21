using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmGroupOperator
    {
        public int Position { get; set; }
        public string Operator { get; set; }
        public string ID { get; set; }

        public SCADAlarmGroupOperator()
        {
            Position = 0;
            Operator = string.Empty;
            ID = string.Empty;
        }
    }
}
