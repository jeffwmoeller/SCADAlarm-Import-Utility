using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCADAlarm_Import_Utility.Model;

namespace SCADAlarm_Import_Utility
{
    class SCADAlarmToWIN911
    {
        static WIN911Config win911Config = new WIN911Config(@"Configuration Files\SCADAlarm.mdb");

        static public void XlateSCADAlarm(
            List<SCADAlarmOperator> scadalarmOperators,
            List<SCADAlarmGroup> scadalarmGroups,
            List<SCADAlarmServer> scadalarmServers,
            List<SCADAlarmTag> scadalarmTags)
        {
            // Create an empty database
            win911Config.CreateEmptyDatabase();

            // Create a Data Source to associate with groups
            win911Config.AddDataSource("GroupDataSource", "Excel", "Sheet1", "DDE Server");

            XlateGroups(scadalarmGroups);
            XlateOperators(scadalarmOperators);
            XlateServers(scadalarmServers, scadalarmTags);

            win911Config.Commit();
        }

        static void XlateGroups(List<SCADAlarmGroup> scadalarmGroups)
        {
            // SCADAlarm first notifies members of a specified group and then members of the Backup group.
            // Collect and sort the Backup group operators so they can be added to all other groups.
            List<SCADAlarmGroupOperator> backupOperators = scadalarmGroups.Find(g => g.Name == "Backup").Operators;
            backupOperators.OrderBy(o => o.Position);

            foreach (SCADAlarmGroup scadalarmGroup in scadalarmGroups)
            {
                win911Config.AddGroup(
                    scadalarmGroup.Name,
                    true,
                    true,
                    true,
                    WIN911Config.PopupStyle.Box,
                    5,
                    "GroupDataSource");

                XlateGroupOperators(scadalarmGroup.Name, scadalarmGroup.Operators, backupOperators);
            }
        }

        /// <summary>
        /// Xlate Group Operators to NameList
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="scadalarmGroupOperators"></param>
        /// <param name="backupOperators"></param>
        static void XlateGroupOperators(
            string groupName,
            List<SCADAlarmGroupOperator> scadalarmGroupOperators,
            List<SCADAlarmGroupOperator> backupOperators)
        {
            List<SCADAlarmGroupOperator> groupOperators = new List<SCADAlarmGroupOperator>(scadalarmGroupOperators);

            groupOperators.OrderBy(o => o.Position);

            foreach (SCADAlarmGroupOperator backupOperator in backupOperators)
            {
                if (groupOperators.Find(o => o.Operator == backupOperator.Operator) == null) groupOperators.Add(backupOperator);
            }

            foreach (SCADAlarmGroupOperator groupOperator in groupOperators)
            {
                win911Config.AddNameList(groupName, groupOperator.Operator);
            }
        }

        /// <summary>
        /// Xlate Operator to PhoneName
        /// </summary>
        /// <param name="scadalarmOperators"></param>
        static void XlateOperators(List<SCADAlarmOperator> scadalarmOperators)
        {
            foreach (SCADAlarmOperator scadalarmOperator in scadalarmOperators)
            {
                win911Config.AddPhoneName(
                    scadalarmOperator.Name,
                    scadalarmOperator.GreetingSpeech,
                    scadalarmOperator.ID,
                    scadalarmOperator.ID);

                foreach (SCADAlarmContact contact in scadalarmOperator.Contacts) XlateContact(scadalarmOperator, contact);
            }
        }

        /// <summary>
        /// Xlate Contact to PhoneNumber
        /// </summary>
        /// <param name="scadalarmOperator"></param>
        /// <param name="contact"></param>
        static void XlateContact(SCADAlarmOperator scadalarmOperator, SCADAlarmContact contact)
        {
            string phoneNumber = string.Empty;
            string serviceNumber = string.Empty;
            WIN911Config.ConnectionType connectionType = 0;

            switch (contact.Type)
            {
                case SCADAlarmContact.ContactType.AlphaPager:
                    string[] numberService = contact.AccessString.Split('@');
                    phoneNumber = numberService[0];
                    serviceNumber = numberService[1].Replace("-", "");
                    connectionType = WIN911Config.ConnectionType.AlphaPager;
                    break;
                case SCADAlarmContact.ContactType.Email:
                    serviceNumber = contact.AccessString;
                    connectionType = WIN911Config.ConnectionType.EMail;
                    break;
                case SCADAlarmContact.ContactType.NumericPager:
                    phoneNumber = contact.AccessString.Replace("-", "");
                    connectionType = WIN911Config.ConnectionType.NumericPager;
                    break;
                case SCADAlarmContact.ContactType.VoicePager:
                    phoneNumber = contact.AccessString.Replace("-", "");
                    connectionType = WIN911Config.ConnectionType.VoicePager;
                    break;
                case SCADAlarmContact.ContactType.VoicePhone:
                    phoneNumber = contact.AccessString.Replace("-", "");
                    connectionType = WIN911Config.ConnectionType.Voice;
                    break;
            }

            win911Config.AddPhoneNumber(
                scadalarmOperator.Name,
                phoneNumber,
                serviceNumber,
                connectionType,
                "Always (24 hours - all week)",
                scadalarmOperator.Contacts.IndexOf(contact));
        }

        static void XlateServers(List<SCADAlarmServer> scadalarmServers, List<SCADAlarmTag> scadalarmTags)
        {
            // Create a Group to associate with numeric tags
            win911Config.AddGroup("DataTags", false, false, false, WIN911Config.PopupStyle.None, 0, "GroupDataSource");

            // Create a Group to associate with acknowledgement tags
            win911Config.AddGroup("AckTags", false, false, false, WIN911Config.PopupStyle.None, 0, "GroupDataSource");

            // ArchestrA servers are ignored for now
            XlateDdeServers(scadalarmServers, scadalarmTags);
            XlateIntouchServers(scadalarmServers, scadalarmTags);
        }

        static void XlateDdeServers(List<SCADAlarmServer> scadalarmServers, List<SCADAlarmTag> scadalarmTags)
        {
            // Collect all DDE servers
            var servers = from server in scadalarmServers where IsDdeServer(server) select server;

            foreach (SCADAlarmServer server in servers)
            {
                // Xlate Server to Data Source
                // Note: there may be no tags/alarms associated with this server
                win911Config.AddDataSource(
                    server.Name,
                    IsLocalServer(server)?server.ApplicationGalaxy:@"\\"+server.Node+@"\"+server.ApplicationGalaxy,
                    server.Topic,
                    "DDE Server");

                // Collect all the tags on this server
                var tags = 
                    from tag in scadalarmTags
                    where tag.Server == server.Name
                    select tag;

                foreach (SCADAlarmTag tag in tags)
                {
                    if (IsAnalog(tag)) AddAnalogTag(tag, server.Name);
                    else if (IsDigitalTag(tag)) AddDigitalTag(tag, server.Name);
                    else if (IsDigitalAlarm(tag)) AddDigitalAlarm(tag, server.Name);
                    else if (IsText(tag)) AddTextTag(tag, server.Name);

                    if (HasAckTag(tag)) AddAckTag(tag, server, server.Name);
                }
            }
        }

        static void XlateIntouchServers(List<SCADAlarmServer> scadalarmServers, List<SCADAlarmTag> scadalarmTags)
        {
            // Collect all InTouch servers
            var servers = from server in scadalarmServers where IsInTouchServer(server) select server;

            foreach (SCADAlarmServer server in servers)
            {
                string serverName ;

                // Xlate Server to Data Source
                // Note: there may be no tags/alarms associated with this server
                if (IsLocalServer(server))
                {
                    serverName = "InTouch Direct Connect";

                    // Add a local InTouch server for InTouch alarms.
                    win911Config.AddDataSource( 
                        "InTouch Direct Connect", 
                        "InTouch Direct Connect",
                        "InTouch Direct Connect", 
                        "InTouch Direct Connect" );
                }
                else
                {
                    serverName = server.Name;

                    // Add a DDE server for remote InTouch servers.
                    win911Config.AddDataSource(
                        server.Name,
                        @"\\" + server.Node + @"\VIEW",
                        "TAGNAME",
                        "DDE Server");
                }

                // Collect all the tags on this server
                var tags = 
                    from tag in scadalarmTags
                    where tag.Server == server.Name
                    select tag;

                foreach (SCADAlarmTag tag in tags)
                {
                    if (IsIntouchAlarm(tag) && IsLocalServer(server)) AddInTouchAlarm(tag);
                    else if (IsAnalog(tag)) AddAnalogTag(tag, serverName);
                    else if (IsDigitalTag(tag)) AddDigitalTag(tag, serverName);
                    else if (IsDigitalAlarm(tag)) AddDigitalAlarm(tag, serverName);
                    else if (IsText(tag)) AddTextTag(tag, serverName);

                    if (HasAckTag(tag)) AddAckTag(tag, server, serverName);
                }
            }
        }

        static void InitTagname(SCADAlarmDataSet.TagnameRow tagname, SCADAlarmTag tag)
        {
        }

        static void AddTagname(SCADAlarmTag tag, WIN911Config.TagValueType tagValueType)
        {
            SCADAlarmDataSet.TagnameRow tagname = win911Config.NewTagname();

            InitTagname(tagname, tag);

            tagname.szTagname = tag.Name;
            tagname.szDescription = tag.Description;
            tagname.szGroupName = IsAlarm(tag) ? tag.CallGroup : "DataTags";
            tagname.szSoundFile = string.Empty;
            tagname.bUseIsWas = IsAlarm(tag);
            tagname.bAnnounceOnly = false;
            tagname.bAckOnReturn = tag.AckOnClear;
            tagname.nType = (short)tagValueType;
            tagname.bBypass = false;
            tagname.bChangable = IsTag(tag);

            win911Config.AddTagname(tagname);
        }

        static void InitAnalog(SCADAlarmDataSet.AnalogRow analog, SCADAlarmTag tag, string serverName)
        {
            analog.szTagname = tag.Name;
            analog.szAccess_Name = serverName;
            analog.bUseTagname = tag.Name == tag.Item;
            analog.szItemName = tag.Item;
            analog.bScaling = (byte)WIN911Config.Scaling.None;
            analog.szEngineeringUnits = (tag.UnitsSound == string.Empty) ? string.Empty : tag.UnitsSound.Split('.')[0];
            analog.bAlarm1Enabled = false;
            analog.bAlarm2Enabled = false;
            analog.bAlarm3Enabled = false;
            analog.bAlarm4Enabled = false;
            analog.szAlarm1SoundFile = tag.ActiveSound;
            analog.szAlarm2SoundFile = tag.ActiveSound;
            analog.szAlarm3SoundFile = tag.ActiveSound;
            analog.szAlarm4SoundFile = tag.ActiveSound;
            analog.szNowNormalSound = tag.InactiveSound;
            analog.szEngineeringSound = (tag.UnitsSound == string.Empty) ? string.Empty : tag.UnitsSound.Split('.')[0];
            analog.bAlarm1Priority = (byte)Priority(tag);
            analog.bAlarm2Priority = (byte)Priority(tag);
            analog.bAlarm3Priority = (byte)Priority(tag);
            analog.bAlarm4Priority = (byte)Priority(tag);
            analog.dwInitialValue = InitialValue(tag);
            analog.dwMinimumEU = 0;
            analog.dwMaximumEU = 1;
            analog.dwMinimumRaw = 0;
            analog.dwMaximumRaw = 1;
            analog.dwAlarm1Limit = 0;
            analog.dwAlarm2Limit = 0;
            analog.dwAlarm3Limit = 0;
            analog.dwAlarm4Limit = 0;
            analog.dwAlarmDeadband = 0;
            analog.dwMinimumChange = tag.ChangeMin;
            analog.dwMaximumChange = tag.ChangeMax;
            analog.bSigned = true;
            analog.sResolution = 32;
            analog.sDigits = (byte)tag.Precision;
        }

        static void AddAnalogTag(SCADAlarmTag tag, string serverName)
        {
            AddTagname(tag, WIN911Config.TagValueType.Analog);

            SCADAlarmDataSet.AnalogRow analog = win911Config.NewAnalog();

            InitAnalog(analog, tag, serverName);

            win911Config.AddAnalog(analog);
        }

        static void AddInTouchAnalogAlarm(SCADAlarmTag tag, WIN911Config.AnalogAlarmType analogAlarmType)
        {
            AddTagname(tag, WIN911Config.TagValueType.Analog);

            SCADAlarmDataSet.AnalogRow analog = win911Config.NewAnalog();

            InitAnalog(analog, tag, "InTouch Direct Connect");

            switch (analogAlarmType)
            {
                case WIN911Config.AnalogAlarmType.HIHI: analog.bAlarm1Enabled = true; break;
                case WIN911Config.AnalogAlarmType.HI: analog.bAlarm2Enabled = true; break;
                case WIN911Config.AnalogAlarmType.LO: analog.bAlarm3Enabled = true; break;
                case WIN911Config.AnalogAlarmType.LOLO: analog.bAlarm4Enabled = true; break;
            }

            win911Config.AddAnalog(analog);
        }

        static void InitDigital(SCADAlarmDataSet._Digital__Bitpick_Row digital, SCADAlarmTag tag, string serverName)
        {
            digital.szTagname = tag.Name;
            digital.szAccess_Name = serverName;
            digital.bUseTagname = tag.Name == tag.Item;
            digital.szItemName = tag.Item;
            digital.bInitialValue = InitialValue(tag) == 1 ? true : false;
            digital.bAlarmEnabled = false;
            digital.bAlarmOn = ActiveValue(tag) == 1 ? true : false;
            digital.sPriority = (byte)Priority(tag);
            digital.szOnSoundFile = tag.ActiveSound;
            digital.szOffSoundFile = tag.InactiveSound;
            digital.szOnText = tag.ActiveText;
            digital.szOffText = tag.InactiveText;
        }

        static void AddDigitalTag(SCADAlarmTag tag, string serverName)
        {
            AddTagname(tag, WIN911Config.TagValueType.Bit1);

            SCADAlarmDataSet._Digital__Bitpick_Row digital = win911Config.NewDigital();

            InitDigital(digital, tag, serverName);

            win911Config.AddDigital(digital);
        }

        static void AddDigitalAlarm(SCADAlarmTag tag, string serverName)
        {
            AddTagname(tag, WIN911Config.TagValueType.Bit1);

            SCADAlarmDataSet._Digital__Bitpick_Row digital = win911Config.NewDigital();

            InitDigital(digital, tag, serverName);

            digital.bAlarmEnabled = true;

            win911Config.AddDigital(digital);
        }

        static void AddTextTag(SCADAlarmTag tag, string serverName)
        {
            AddTagname(tag, WIN911Config.TagValueType.Text);

            SCADAlarmDataSet._Digital__Bitpick_Row digital = win911Config.NewDigital();

            InitDigital(digital, tag, serverName);

            win911Config.AddDigital(digital);
        }

        static void AddInTouchAlarm(SCADAlarmTag tag)
        {
            if (tag.Item.EndsWith(".hihistatus")) AddInTouchAnalogAlarm(tag, WIN911Config.AnalogAlarmType.HIHI);
            else if (tag.Item.EndsWith(".histatus")) AddInTouchAnalogAlarm(tag, WIN911Config.AnalogAlarmType.HI);
            else if (tag.Item.EndsWith(".lostatus")) AddInTouchAnalogAlarm(tag, WIN911Config.AnalogAlarmType.LO);
            else if (tag.Item.EndsWith(".lolostatus")) AddInTouchAnalogAlarm(tag, WIN911Config.AnalogAlarmType.LOLO);
            else AddDigitalAlarm(tag, "InTouch Direct Connect");
        }

        static void AddAckTagname(SCADAlarmTag tag, WIN911Config.TagValueType tagValueType)
        {
            SCADAlarmDataSet.TagnameRow tagname = win911Config.NewTagname();

            tagname.szTagname = tag.Name + " AckTag";
            tagname.szDescription = tag.Description + " AckTag";
            tagname.szGroupName = "AckTags";
            tagname.szSoundFile = tag.Description == string.Empty ? tag.AckTag + " AckTag" : tag.Description + " AckTag";
            tagname.bUseIsWas = false;
            tagname.bAnnounceOnly = false;
            tagname.bAckOnReturn = false;
            tagname.nType = (short)tagValueType;
            tagname.bBypass = false;
            tagname.bChangable = true;

            win911Config.AddTagname(tagname);
        }

        static void InitAnalogAck(SCADAlarmDataSet.AnalogRow analog, SCADAlarmTag tag, string serverName)
        {
            analog.szTagname = tag.Name + " AckTag";
            analog.szAccess_Name = serverName;
            analog.bUseTagname = false;
            analog.szItemName = tag.AckTag;
            analog.bScaling = (byte)WIN911Config.Scaling.None;
            analog.szEngineeringUnits = string.Empty;
            analog.bAlarm1Enabled = false;
            analog.bAlarm2Enabled = false;
            analog.bAlarm3Enabled = false;
            analog.bAlarm4Enabled = false;
            analog.szAlarm1SoundFile = string.Empty;
            analog.szAlarm2SoundFile = string.Empty;
            analog.szAlarm3SoundFile = string.Empty;
            analog.szAlarm4SoundFile = string.Empty;
            analog.szNowNormalSound = string.Empty;
            analog.szEngineeringSound = string.Empty;
            analog.bAlarm1Priority = (byte)WIN911Config.Priority.Low;
            analog.bAlarm2Priority = (byte)WIN911Config.Priority.Low;
            analog.bAlarm3Priority = (byte)WIN911Config.Priority.Low;
            analog.bAlarm4Priority = (byte)WIN911Config.Priority.Low;
            analog.dwInitialValue = 0;
            analog.dwMinimumEU = 0;
            analog.dwMaximumEU = 1;
            analog.dwMinimumRaw = 0;
            analog.dwMaximumRaw = 1;
            analog.dwAlarm1Limit = 0;
            analog.dwAlarm2Limit = 0;
            analog.dwAlarm3Limit = 0;
            analog.dwAlarm4Limit = 0;
            analog.dwAlarmDeadband = 0;
            analog.dwMinimumChange = 0;
            analog.dwMaximumChange = 100000;
            analog.bSigned = true;
            analog.sResolution = 32;
            analog.sDigits = 0;
        }

        static void AddAnalogAckTag(SCADAlarmTag tag, SCADAlarmServer server, string serverName)
        {
            AddAckTagname(tag, WIN911Config.TagValueType.Analog);

            SCADAlarmDataSet.AnalogRow analog = win911Config.NewAnalog();

            InitAnalogAck(analog, tag, serverName);

            win911Config.AddAnalog(analog);
        }

        static void InitDigitalAck(SCADAlarmDataSet._Digital__Bitpick_Row digital, SCADAlarmTag tag, string serverName)
        {
            digital.szTagname = tag.Name + " AckTag";
            digital.szAccess_Name = serverName;
            digital.bUseTagname = false;
            digital.szItemName = tag.AckTag;
            digital.bInitialValue = false;
            digital.bAlarmEnabled = false;
            digital.sPriority = (byte)WIN911Config.Priority.Low;
            digital.szOnSoundFile = "Acked";
            digital.szOffSoundFile = "Unacked";
            digital.szOffText = "Unacked";
        }

        static void AddDigitalAckTag(SCADAlarmTag tag, SCADAlarmServer server, string serverName)
        {
            AddAckTagname(tag, WIN911Config.TagValueType.Bit1);

            SCADAlarmDataSet._Digital__Bitpick_Row digital = win911Config.NewDigital();

            InitDigitalAck(digital, tag, serverName);

            digital.bAlarmOn = server.AckString == "1" ? true : false;
            digital.szOnText = "Acked";

            win911Config.AddDigital(digital);
        }

        static void AddTextAckTag(SCADAlarmTag tag, SCADAlarmServer server, string serverName)
        {
            AddAckTagname(tag, WIN911Config.TagValueType.Text);

            SCADAlarmDataSet._Digital__Bitpick_Row digital = win911Config.NewDigital();

            InitDigitalAck(digital, tag, serverName);

            digital.bAlarmOn = true;
            digital.szOnText = server.AckString;

            win911Config.AddDigital(digital);
        }

        static void AddAckTag(SCADAlarmTag tag, SCADAlarmServer server, string serverName)
        {
            if (IsBinary(server.AckString)) AddDigitalAckTag(tag, server, serverName);
            else if (IsNumeric(server.AckString)) AddAnalogAckTag(tag, server, serverName);
            else AddTextAckTag(tag, server, serverName);
        }

        static bool IsDdeServer(SCADAlarmServer server) { return server.ServerEnabled && server.Type == SCADAlarmServer.ServerType.DDE; }
        static bool IsInTouchServer(SCADAlarmServer server) { return server.ServerEnabled && server.Type == SCADAlarmServer.ServerType.InTouch; }
        static bool IsArchestraServer(SCADAlarmServer server) { return server.ServerEnabled && server.Type == SCADAlarmServer.ServerType.ArchestrA; }

        static bool IsLocalServer(SCADAlarmServer server) { return server.Node == string.Empty; }
        static bool IsRemoteServer(SCADAlarmServer server) { return server.Node != string.Empty; }

        static bool IsNumeric(string stringValue) { double doubleValue; return double.TryParse(stringValue, out doubleValue); }
        static bool IsBinary(string s) { int i; if (int.TryParse(s, out i)) return i == 0 || i == 1; else return false; }

        static bool IsAlarm(SCADAlarmTag tag) { return tag.DialOutEnabled; }
        static bool IsTag(SCADAlarmTag tag) { return !tag.DialOutEnabled; }

        static bool IsNumericTag(SCADAlarmTag tag) { return IsTag(tag) && (tag.OnStateValue == string.Empty); }
        static bool IsAnalogTag(SCADAlarmTag tag) { return IsNumericTag(tag) || (IsTag(tag) && IsNumeric(tag.OnStateValue) && !IsBinary(tag.OnStateValue)); }
        static bool IsAnalogAlarm(SCADAlarmTag tag) { return IsAlarm(tag) && IsNumeric(tag.OnStateValue) && !IsBinary(tag.OnStateValue); }
        static bool IsAnalog(SCADAlarmTag tag) { return IsAnalogTag(tag) || IsAnalogAlarm(tag); }

        static bool IsDigitalTag(SCADAlarmTag tag) { return IsTag(tag) && IsBinary(tag.OnStateValue); }
        static bool IsDigitalAlarm(SCADAlarmTag tag) { return IsAlarm(tag) && IsBinary(tag.OnStateValue); }
        static bool IsDigital(SCADAlarmTag tag) { return IsDigitalTag(tag) || IsDigitalAlarm(tag); }

        static bool IsTextTag(SCADAlarmTag tag) { return IsTag(tag) && !IsNumeric(tag.OnStateValue); }
        static bool IsTextAlarm(SCADAlarmTag tag) { return IsAlarm(tag) && IsNumeric(tag.OnStateValue); }
        static bool IsText(SCADAlarmTag tag) { return IsTextTag(tag) || IsTextAlarm(tag); }

        static bool IsIntouchAlarm(SCADAlarmTag tag) { return IsDigitalAlarm(tag) && tag.AckTag.EndsWith(".ack") && !tag.OnStateInverted; }

        static bool HasAckTag(SCADAlarmTag tag) { return tag.AckTag != string.Empty; }

        static int InitialValue(SCADAlarmTag tag)
        {
            return (((tag.OnStateValue == "0") && (!tag.OnStateInverted)) || ((tag.OnStateValue == "1") && (tag.OnStateInverted))) ? 1 : 0 ;
        }

        static int ActiveValue(SCADAlarmTag tag)
        {
            return (((tag.OnStateValue == "1") && (!tag.OnStateInverted)) || ((tag.OnStateValue == "0") && (tag.OnStateInverted))) ? 1 : 0 ;
        }

        static WIN911Config.Priority Priority(SCADAlarmTag tag)
        {
            return (tag.Priority < 68) ? WIN911Config.Priority.High : (tag.Priority < 134) ? WIN911Config.Priority.Medium : WIN911Config.Priority.Low;
        }
    }
}
