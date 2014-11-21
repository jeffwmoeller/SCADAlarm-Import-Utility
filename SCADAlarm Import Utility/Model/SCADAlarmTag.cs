using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmTag : SCADAlarmBase
    {
        public static List<SCADAlarmTag> Parse(string[] logicalFiles)
        {
            return ParseSections<SCADAlarmTag>(GetSectionFile(logicalFiles, logicalFileHeader), sectionDelimiter);
        }

        private static string logicalFileHeader = "Alarm/Tag File dump:";
        private static string sectionDelimiter = "Tag ";

        override protected List<LineDelegate> LineDelegates
        {
            get
            {
                return new List<LineDelegate>{
                new LineDelegate( new ParseLineDelegate(ParseSectionHeader), sectionDelimiter),
                new LineDelegate( new ParseLineDelegate(ParseConnection), connectionPrefix),
                new LineDelegate( new ParseLineDelegate(ParseValue), valuePrefix),
                new LineDelegate( new ParseLineDelegate(ParseLogging), loggingPrefix),
                new LineDelegate( new ParseLineDelegate(ParseSounds), soundsPrefix),
                new LineDelegate( new ParseLineDelegate(ParseAlphaPager), alphaPagerPrefix),
                new LineDelegate( new ParseLineDelegate(ParseNumericPager), numericPagerPrefix),
                new LineDelegate( new ParseLineDelegate(ParseCallGroup), callGroupPrefix),
                new LineDelegate( new ParseLineDelegate(ParseAutoAck), autoAckPrefix),
                new LineDelegate( new ParseLineDelegate(ParseNotify), notifyPrefix),
                new LineDelegate( new ParseLineDelegate(ParseSpeakUnits), precisionPrefix),
                new LineDelegate( new ParseLineDelegate(ParseChangeableRange), changeableRangePrefix) };
            }
        }

        private static string connectionPrefix = "     Connection: Get data from ";
        private static string valuePrefix = "     On-state value: ";
        private static string loggingPrefix = "     Logging is ";
        private static string soundsPrefix = "     Speak when CLEAR: ";
        private static string alphaPagerPrefix = "     Alpha Pager OFF state: ";
        private static string numericPagerPrefix = "     Numeric Pager Alarm Number ";
        private static string callGroupPrefix = "     Call ";
        private static string autoAckPrefix = "     Auto-ack on clear is ";
        private static string notifyPrefix = "     Notify on clear is ";
        private static string precisionPrefix = "     Speak Units:  to ";
        private static string changeableRangePrefix = "     Limit phone-changeable range to ";

        public string Name { get; set; }
        public string Description { get; set; }
        public bool DialOutEnabled { get; set; }
        public int Priority { get; set; }
        public int Delay { get; set; }
        public string Server { get; set; }
        public string Item { get; set; }
        public string OnStateValue { get; set; }
        public bool OnStateInverted { get; set; }
        public string AckTag { get; set; }
        public bool LoggingEnabled { get; set; }
        public string InactiveSound { get; set; }
        public string ActiveSound { get; set; }
        public string InactiveText { get; set; }
        public string ActiveText { get; set; }
        public string NumericPagerID { get; set; }
        public string NumericPagerInactive { get; set; }
        public string NumericPagerActive { get; set; }
        public string CallGroup { get; set; }
        public string NotifyGroup { get; set; }
        public bool AckOnClear { get; set; }
        public bool AckOnDelivery { get; set; }
        public bool NotifyOnClear { get; set; }
        public bool NotifyOnAck { get; set; }
        public string UnitsSound { get; set; }
        public int Precision { get; set; }
        public double ChangeMin { get; set; }
        public double ChangeMax { get; set; }

        public SCADAlarmTag()
        {
        Name = string.Empty;
        Description = string.Empty;
        DialOutEnabled = false;
        Priority = 0;
        Delay = 0;
        Server = string.Empty;
        Item = string.Empty;
        OnStateValue = string.Empty;
        OnStateInverted = false;
        AckTag = string.Empty;
        LoggingEnabled = false;
        InactiveSound = string.Empty;
        ActiveSound = string.Empty;
        InactiveText = string.Empty;
        ActiveText = string.Empty;
        NumericPagerID = string.Empty;
        NumericPagerInactive = string.Empty;
        NumericPagerActive = string.Empty;
        CallGroup = string.Empty;
        NotifyGroup = string.Empty;
        AckOnClear = false;
        AckOnDelivery = false;
        NotifyOnClear = false;
        NotifyOnAck = false;
        UnitsSound = string.Empty;
        Precision = 0 ;
        ChangeMin = 0;
        ChangeMax = 0;
        }

        // tag lines are one of these forms:
        //    "Tag "NAME" (DESCRIPTION);  dial-out disabled"
        //    "Tag "NAME" (DESCRIPTION);  dial-out enabled; priority High, delay 0 sec"
        // Name = "NAME", Description = "DESCRIPTION", DialOutEnabled = false/true, Priority = 1, Delay = 0
        // Description is optional and can be empty ()
        // DialOutEnabled = false for "dial-out disabled", true for "dial-out enabled"
        // Priority ranges from 1 to 200, High = 1, Low = 200
        private void ParseSectionHeader(string line)
        {
            // parts[0]=NAME
            // parts[1]=DESCRIPTION
            // parts[2]=disabled
            // parts[3]="High"
            // parts[4]="0"
            string[] parts = line.Split(new string[] { "Tag \"", "\" (",";  dial-out ","; priority ",", delay ", " sec" }, StringSplitOptions.RemoveEmptyEntries);
            Name = parts[0];
            Description = parts[1].Replace(")", "");
            DialOutEnabled = parts[2].Contains("enabled");

            if (DialOutEnabled)
            {
                if (parts[3] == "High") Priority = 1;
                else if (parts[3] == "Low") Priority = 200;
                else Priority = int.Parse(parts[3]);

                Delay = int.Parse(parts[4]);
            }
        }

        // "     Connection: Get data from SERVER NAME (DDE:VIEW|TAGNAME) item: ITEM"
        // Server=SERVER NAME. Item=ITEM
        private void ParseConnection(string line)
        {
            // parts[0]="SERVER NAME"
            // parts[1]="DDE:VIEW|TAGNAME"
            // parts[2]="ITEM"
            string[] parts = line.Split(new string[] { "     Connection: Get data from ", " (", ") item: " }, StringSplitOptions.RemoveEmptyEntries);
            Server = parts[0];
            Item = parts[2];
        }

        // "     On-state value: "1"  Logging is disabled" or
        // "     On-state value: anything except "1"  Logging is disabled" or
        // "     On-state value: "1"  Ack tag: tank.ack  Logging is disabled" or
        // "     On-state value: anything except "1"  Ack tag: tank.ack  Logging is disabled"
        // ActiveValue="1", AckTag="tank.ack", LoggingEnabled=false/true
        // ActiveValueInverse is true if "anything except", else false
        // LoggingEnabled is false if "Logging is disabled", true if "Logging is enabled"
        private void ParseValue(string line)
        {
            // parts[0]="     On-state value:" or "     On-state value: anything except"
            // parts[1]="1"
            // parts[2]="disabled" or "tank.ack"
            // parts[3]="disabled"
            string[] parts = line.Split(new string[] { " \"", "\"  Ack tag: ", "\"  Logging is ", "  Logging is " }, StringSplitOptions.RemoveEmptyEntries);

            OnStateInverted = parts[0].Contains("anything except");
            OnStateValue = parts[1];

            if (parts.Count() == 3)
            {
                LoggingEnabled = parts[2].Contains("enabled");
            }
            else
            {
                AckTag = parts[2];
                LoggingEnabled = parts[3].Contains("enabled");
            }
        }

        // "     Logging is disabled"
        // LoggingEnabled is false if "Logging is disabled", true if "Logging is enabled"
        private void ParseLogging(string line)
        {
            LoggingEnabled = line.Contains("Logging is enabled");
        }

        // "     Speak when CLEAR: Z_CLEAR.wav / in ALARM: Z_ALARM.wav"
        // InactiveSound="Z_CLEAR.wav". ActiveSound="Z_ALARM.wav"
        private void ParseSounds(string line)
        {
            // parts[0]=" Z_CLEAR.wav "
            // parts[1]=" Z_ALARM.wav"
            string[] parts = line.Split(new string[] {"     Speak when CLEAR: "," / in ALARM: "}, StringSplitOptions.RemoveEmptyEntries);
            InactiveSound = parts[0];
            ActiveSound = parts[1];
        }

        // "     Alpha Pager OFF state: "Normal"  ON state: "Alarm""
        // InactiveText="Normal", ActiveText="Alarm"
        private void ParseAlphaPager(string line)
        {
            // parts[0]="     Alpha Pager OFF state: "
            // parts[1]="Normal"
            // parts[2]="  ON state: "
            // parts[3]="Alarm"
            string[] parts = line.Split(new char[] {'"'}, StringSplitOptions.RemoveEmptyEntries);
            InactiveText = parts[1];
            ActiveText = parts[3];
        }

        // "     Numeric Pager Alarm Number "011"  OFF state: "0"  ON state: "1""
        // NumericPagerID="011", NumericPagerInactive="0", NumericPagerActive="1"
        private void ParseNumericPager(string line)
        {
            // parts[0]="     Numeric Pager Alarm Number "
            // parts[1]="011"
            // parts[2]="  OFF state: "
            // parts[3]="0"
            // parts[4]="  ON state: "
            // parts[5]="1"
            string[] parts = line.Split(new char[] {'"'}, StringSplitOptions.RemoveEmptyEntries);
            NumericPagerID = parts[1];
            NumericPagerInactive = parts[3];
            NumericPagerActive = parts[5];
        }

        // "     Call "Backup" group" or
        // "     Call "Backup" group; also notify "Operator" group"
        // CallGroup="Backup", NotifyGroup="Operator"
        private void ParseCallGroup(string line)
        {
            // parts[0]="     Call "
            // parts[1]="Backup"
            // parts[2]=" group" or " group; also notify "
            // parts[3]="Operator"
            // parts[4]=" group"
            string[] parts = line.Split(new char[] {'"'}, StringSplitOptions.RemoveEmptyEntries);
            CallGroup = parts[1];
            if (parts.Count() == 5) NotifyGroup = parts[3];
        }

        // "     Auto-ack on clear is disabled;  Auto-ack on delivery is disabled"
        // AckOnClear=false, AckOnDelivery=false
        private void ParseAutoAck(string line)
        {
            AckOnClear = line.Contains("Auto-ack on clear is enabled");
            AckOnDelivery = line.Contains("Auto-ack on delivery is enabled");
        }

        // "     Notify on clear is disabled;  Notify on Ack is disabled"
        // NotifyOnClear=false, NotifyOnAck=false
        private void ParseNotify(string line)
        {
            NotifyOnClear = line.Contains("Notify on clear is enabled");
            NotifyOnAck = line.Contains("Notify on Ack is enabled");
        }

        // "     Speak Units:  to 2 Decimal Places" or
        // "     Speak Units: feet.wav to 2 Decimal Places"
        // Precision=2
        private void ParseSpeakUnits(string line)
        {
            // parts[0]="feet.wav" or "2"
            // parts[1]="2"
            string[] parts = line.Split(
                new string[] { "     Speak Units: "," to ", " Decimal Places" },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Count() == 2) UnitsSound = parts[0];
            Precision = int.Parse((parts.Count() == 1) ? parts[0] : parts[1]);
        }

        // "     Limit phone-changeable range to 0.00-100000.00"
        // ChangeMin=0.00, ChangeMax=100000.00
        private void ParseChangeableRange(string line)
        {
            string[] parts = line.Split(
                new string[] { "     Limit phone-changeable range to " },
                StringSplitOptions.RemoveEmptyEntries);
            string[] minMax = parts[0].Split('-');
            ChangeMin = double.Parse(minMax[0]);
            ChangeMax = double.Parse(minMax[1]);
        }
    }
}
