using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmContact
    {
        public int Position { get; set; }
        public string Comment { get; set; }
        public ContactType Type { get; set; }
        public string AccessString { get; set; }

        // contact lines are of the form "     #1 some comment (type: accessString)." or "     #1 type: accessString."
        // ID = 1, Comment = "some comment", Type = type, AccessString = accessString
        public SCADAlarmContact(string line)
        {
            // Parse the ContactMethod's ID
            Position = int.Parse(line.Substring(6, 1));

            // Parse the ContactMethod's Comment
            Comment = (line.EndsWith(").")) ? line.Substring(8, line.LastIndexOf('(') - 9) : string.Empty;

            // Parse the ContactMethod's Type
            if      (line.Contains(SCADAlarmContact.GetEnumDescription(SCADAlarmContact.ContactType.VoicePhone))) Type = SCADAlarmContact.ContactType.VoicePhone;
            else if (line.Contains(SCADAlarmContact.GetEnumDescription(SCADAlarmContact.ContactType.NumericPager))) Type = SCADAlarmContact.ContactType.NumericPager;
            else if (line.Contains(SCADAlarmContact.GetEnumDescription(SCADAlarmContact.ContactType.AlphaPager))) Type = SCADAlarmContact.ContactType.AlphaPager;
            else if (line.Contains(SCADAlarmContact.GetEnumDescription(SCADAlarmContact.ContactType.VoicePager))) Type = SCADAlarmContact.ContactType.VoicePager;
            else if (line.Contains(SCADAlarmContact.GetEnumDescription(SCADAlarmContact.ContactType.Email))) Type = SCADAlarmContact.ContactType.Email;
            else Type = SCADAlarmContact.ContactType.Unknown;

            // Parse the ContactMethod's AccessString
            AccessString = line.Substring(line.LastIndexOf(": ") + 2).TrimEnd(')', '.');
        }

        public enum ContactType
        {
            [Description("Unknown")]      Unknown,
            [Description("Voice Phone")]  VoicePhone,
            [Description("Numeric Pgr")]  NumericPager,
            [Description("Alpha. Pager")] AlphaPager,
            [Description("Voice Pager")]  VoicePager,
            [Description("eMail")]        Email
        };

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
    }
}
