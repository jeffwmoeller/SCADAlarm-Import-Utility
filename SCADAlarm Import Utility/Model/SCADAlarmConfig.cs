using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmConfig
    {
        public List<SCADAlarmOperator> GetOperators() { return SCADAlarmOperator.Parse(sectionFiles); }
        public List<SCADAlarmGroup> GetGroups() { return SCADAlarmGroup.Parse(sectionFiles); }
        public List<SCADAlarmServer> GetServers() { return SCADAlarmServer.Parse(sectionFiles); }
        public List<SCADAlarmTag> GetTags() { return SCADAlarmTag.Parse(sectionFiles); }

        private string[] sectionFiles = null;

        public SCADAlarmConfig(string configFile)
        {
            using (StreamReader sr = new StreamReader(configFile))
            {
                // read the entire file into a buffer
                StringBuilder file = new StringBuilder();
                char[] buffer = new char[32768];
                while (sr.ReadBlock(buffer, 0, buffer.Length) > 0)
                {
                    file.Append(buffer);
                    buffer = new char[32768];
                }

                // Replace CR LF with LF to ease splitting logical files into sections
                file.Replace("\r\n", "\n");

                // Separate the contents of the file into logical files
                // Logical files are seperated by formfeeds
                sectionFiles = file.ToString().Split('\f');

                // If this is not a SCADAlarm Version 6 file ...
                if (!sectionFiles[0].StartsWith("SCADAlarm Advanced Telephonic Dialer  Version 6"))
                {
                    throw(new ApplicationException("Not a SCADAlarm V6 Configuration Listing file."));
                }
            }
        }
    }
}
