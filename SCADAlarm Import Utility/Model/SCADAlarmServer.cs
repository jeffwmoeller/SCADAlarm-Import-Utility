using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmServer : SCADAlarmBase
    {
        public static List<SCADAlarmServer> Parse(string[] logicalFiles)
        {
            return ParseSections<SCADAlarmServer>(GetSectionFile(logicalFiles, logicalFileHeader), sectionDelimiter);
        }

        private static string logicalFileHeader = "Data Server File Dump:";
        private static string sectionDelimiter = "Data Server [";

        override protected List<LineDelegate> LineDelegates
        {
            get
            {
                return new List<LineDelegate>{
                new LineDelegate( new ParseLineDelegate(ParseSectionHeader), sectionDelimiter),
                new LineDelegate( new ParseLineDelegate(ParseInTouchPath), inTouchPathPrefix),
                new LineDelegate( new ParseLineDelegate(ParseServerEnabled), serverEnabledPrefix),
                new LineDelegate( new ParseLineDelegate(ParseAckString), ackStringPrefix),
                new LineDelegate( new ParseLineDelegate(ParseAckResetEnabled), resetEnablePrefix),
                new LineDelegate( new ParseLineDelegate(ParseAckResetString), resetStringPrefix) };
            }
        }

        private static string inTouchPathPrefix = "     Path to InTouch application is ";
        private static string serverEnabledPrefix = "     This server is ";
        private static string ackStringPrefix = "     String to send for Acknowledgment: ";
        private static string resetEnablePrefix = "     Acknowledgment-Tag-reset pokes to this server are ";
        private static string resetStringPrefix = "     String to send to reset Acknowledgment: ";

        public string Name { get; set; }
        public ServerType Type { get; set; }
        public string Node { get; set; }
        public string ApplicationGalaxy { get; set; }
        public string Topic { get; set; }
        public string InTouchPath { get; set; }
        public bool ServerEnabled { get; set; }
        public string AckString { get; set; }
        public bool AckResetEnabled { get; set; }
        public string AckResetString { get; set; }

        public SCADAlarmServer()
        {
            Name = string.Empty;
            Type = ServerType.Unknown;
            Node = string.Empty;
            ApplicationGalaxy = string.Empty;
            Topic = string.Empty;
            InTouchPath = string.Empty;
            ServerEnabled = false;
            AckString = string.Empty;
            AckResetEnabled = false;
            AckResetString = string.Empty;
        }

        // server lines are one of these forms:
        //    "Data Server [1]: Server Name (TYPE:APPLICATION)"
        //    "Data Server [1]: Server Name (TYPE:APPLICATION|TOPIC)"
        //    "Data Server [1]: Server Name (TYPE:\\NODE\APPLICATION)"
        //    "Data Server [1]: Server Name (TYPE:\\NODE\APPLICATION|TOPIC)"
        // Name = "Server Name", Node = "NODE", Type = TYPE, ApplicationGalaxy = "APPLICATION", Topic = "TOPIC"
        // Type can be "DDE", "SuiteLink", or "Galaxy"
        // DDE and SuiteLink have an optional Node and required ApplicationGalaxy and Topic
        // Galaxy has an optional Node and a required ApplicationGalaxy
        private void ParseSectionHeader(string line)
        {
            // parts[0]="Data Server [1]"
            // parts[1]=" Server Name "
            // parts[2]="TYPE"
            // parts[3]="APPLICATION" or "NODE"
            // parts[4]="TOPIC" or "APPLICATION"
            // parts[5]="TOPIC"
            string[] parts = line.Split(new char[]{':', '(', '\\', '|', ')'}, StringSplitOptions.RemoveEmptyEntries);

            // Parse the Name
            Name = parts[1].Trim();

            // Parse the Type
            if (parts[2].Contains(SCADAlarmServer.GetEnumDescription(SCADAlarmServer.ServerType.DDE))) Type = SCADAlarmServer.ServerType.DDE;
            else if (parts[2].Contains(SCADAlarmServer.GetEnumDescription(SCADAlarmServer.ServerType.InTouch))) Type = SCADAlarmServer.ServerType.InTouch;
            else if (parts[2].Contains(SCADAlarmServer.GetEnumDescription(SCADAlarmServer.ServerType.ArchestrA))) Type = SCADAlarmServer.ServerType.ArchestrA;
            else Type = SCADAlarmServer.ServerType.Unknown;

            // If a node was specified ...
            if (line.Contains("\\\\"))
            {
                //  Parse the Node
                Node = parts[3];

                // Parse the ApplicationGalaxy
                ApplicationGalaxy = parts[4];

                // Parse the Topic
                if (parts.Count() == 6) Topic = parts[5];
            }
            else
            {
                // Parse the ApplicationGalaxy
                ApplicationGalaxy = parts[3];

                // Parse the Topic
                if (parts.Count() == 5) Topic = parts[4];
            }
        }

        // "     Path to InTouch application is (on localhost) \." or
        // "     Path to InTouch application is not set."
        private void ParseInTouchPath(string line)
        {
            if (line.Contains("Path to InTouch application is not set.")) InTouchPath = string.Empty;
            else
            {
                // parts[0] = "\\"
                string[] parts = line.Split(new string[] {
                    "     Path to InTouch application is (on localhost) ",
                    "."},
                    StringSplitOptions.RemoveEmptyEntries);

                InTouchPath = parts[0];
            }
        }

        // "     This server is enabled and not responding; used for 1 tag."
        private void ParseServerEnabled(string line)
        {
            ServerEnabled = line.Contains("enabled");
        }

        // "     String to send for Acknowledgment: 1<cr><lf>"
        private void ParseAckString(string line)
        {
            // parts[0]="1<cr><lf>"
            string[] parts = line.Split(new string[] {"     String to send for Acknowledgment: "}, StringSplitOptions.RemoveEmptyEntries);

            AckString = parts[0];
        }

        // "     Acknowledgment-Tag-reset pokes to this server are enabled."
        private void ParseAckResetEnabled(string line)
        {
            AckResetEnabled = line.Contains("enabled");
        }

        // "     String to send to reset Acknowledgment: 0<cr><lf>"
        private void ParseAckResetString(string line)
        {
            // parts[0]="0<cr><lf>"
            string[] parts = line.Split(new string[] { "     String to send to reset Acknowledgment: " }, StringSplitOptions.RemoveEmptyEntries);

            AckResetString = parts[0];
        }

        public enum ServerType
        {
            [Description("Unknown")]    Unknown,
            [Description("DDE")]        DDE,
            [Description("SuiteLink")]  InTouch,
            [Description("Galaxy")]     ArchestrA
        };

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
    }
}
