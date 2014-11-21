using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmGroup : SCADAlarmBase
    {
        public static List<SCADAlarmGroup> Parse(string[] logicalFiles)
        {
            return ParseSections<SCADAlarmGroup>(GetSectionFile(logicalFiles, logicalFileHeader), sectionDelimiter);
        }

        private static string logicalFileHeader = "Group File Dump:";
        private static string sectionDelimiter = "Group [";

        override protected List<LineDelegate> LineDelegates
        {
            get
            {
                return new List<LineDelegate>{
                new LineDelegate( new ParseLineDelegate(ParseSectionHeader), sectionDelimiter),
                new LineDelegate( new ParseLineDelegate(ParseOperator), operatorPrefix) };
            }
        }

        private static string operatorPrefix = "     ";

        public string Name { get; set; }
        public List<SCADAlarmGroupOperator> Operators { get; set; }

        public SCADAlarmGroup()
        {
            Name = string.Empty;
            Operators = new List<SCADAlarmGroupOperator>();
        }

        // group header lines are of the form "Group [0]: groupname" or "Group [1]: groupname (Backup group will be searched after this group.)"
        // Name = "groupname"
        public void ParseSectionHeader(string line)
        {
            string headerLine = line.Contains('(') ?
                line.Remove(line.LastIndexOf('(') - 1) :
                line;
            Name = headerLine.Substring(line.LastIndexOf(':') + 2);
        }

        // group operator lines are of the form "       1 operator (001)"
        // Operator = 'operator', ID = "001"
        private void ParseOperator(string line)
        {
            // Split the group operator line into group properties
            string[] operatorProperties = line.Substring(7).Trim().Split(' ');

            // Parse the GroupOperator's properties
            Operators.Add(new SCADAlarmGroupOperator {
                Position = int.Parse(operatorProperties[0]),
                Operator = operatorProperties[1],
                ID = operatorProperties[2].Substring(1, 3) });
        }
    }
}
