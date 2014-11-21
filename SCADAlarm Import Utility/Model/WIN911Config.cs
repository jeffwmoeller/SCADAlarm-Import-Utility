using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SCADAlarm_Import_Utility.SCADAlarmDataSetTableAdapters;

namespace SCADAlarm_Import_Utility.Model
{
    public class WIN911Config
    {
        private string outputPath;
        public string OutputPath { get; set; }

        private SCADAlarmDataSet scadalarmDataSet;
        public SCADAlarmDataSet ScadalarmDataSet { get; set; }

        /// <summary>
        /// Save the output path and create an empty database
        /// </summary>
        /// <param name="outputFile"></param>
        public WIN911Config(string outputPath)
        {
            OutputPath = outputPath;
            CreateEmptyDatabase();
        }

        /// <summary>
        /// Copy the embedded empty mdb file to the output folder
        /// </summary>
        public void CreateEmptyDatabase()
        {
            // Set the DataDirectory property
            //
            // The DataDirectory is used to build the ConnectionString (as found in App.config)
            AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory + Path.GetDirectoryName(OutputPath));

            // Make sure the output folder exists.
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            using (UnmanagedMemoryStream inputStream = (UnmanagedMemoryStream)Assembly.GetExecutingAssembly().GetManifestResourceStream("SCADAlarm_Import_Utility.SCADAlarm.mdb"))
            using (FileStream outputStream = new FileStream(OutputPath, FileMode.Create)) inputStream.CopyTo(outputStream);

            // Create a working buffer (ConnectionString uses the DataDirectory property)
            scadalarmDataSet = new SCADAlarmDataSet();
        }

        public void AddDDE(
            string szAccess_Name,
            string szApplication_Name,
            string szTopic_Name)
        {
            SCADAlarmDataSet.DDERow ddeRow = scadalarmDataSet.DDE.NewDDERow();
            ddeRow.szAccess_Name = szAccess_Name;
            ddeRow.szApplication_Name = szApplication_Name;
            ddeRow.szTopic_Name = szTopic_Name;
            scadalarmDataSet.DDE.Rows.Add(ddeRow);
        }

        public void AddSource_Types(
            string szAccess_Name,
            string szSource_Type)
        {
            SCADAlarmDataSet.Source_TypesRow source_TypesRow = scadalarmDataSet.Source_Types.NewSource_TypesRow();
            source_TypesRow.szAccess_Name = szAccess_Name;
            source_TypesRow.szSource_Type = szSource_Type;
            scadalarmDataSet.Source_Types.Rows.Add(source_TypesRow);
        }

        public void AddDataSource(
            string szAccess_Name,
            string szApplication_Name,
            string szTopic_Name,
            string szSource_Type)
        {
            AddDDE(szAccess_Name, szApplication_Name, szTopic_Name);
            AddSource_Types(szAccess_Name, szSource_Type);
        }

        public void AddPhoneName(
            string szUserName,
            string szSoundFile,
            string szPassword,
            string szUserID)
        {
            SCADAlarmDataSet.PhoneNameRow phoneNameRow = scadalarmDataSet.PhoneName.NewPhoneNameRow();
            phoneNameRow.szUserName = szUserName;
            phoneNameRow.szSoundFile = szSoundFile;
            phoneNameRow.szPassword = szPassword;
            phoneNameRow.szUserID = szUserID;
            scadalarmDataSet.PhoneName.Rows.Add(phoneNameRow);
        }

        public void AddPhoneNumber(
            string szUserName,
            string szNumber,
            string szService,
            ConnectionType sConnection,
            string szSchedule,
            int lCallOrder)
        {
            SCADAlarmDataSet.PhoneNumberRow phoneNumberRow = scadalarmDataSet.PhoneNumber.NewPhoneNumberRow();
            phoneNumberRow.szUserName = szUserName;
            phoneNumberRow.szNumber = szNumber;
            phoneNumberRow.szService = szService;
            phoneNumberRow.sConnection = (byte)sConnection;
            phoneNumberRow.szSchedule = szSchedule;
            phoneNumberRow.lCallOrder = lCallOrder;
            scadalarmDataSet.PhoneNumber.Rows.Add(phoneNumberRow);
        }

        public void AddGroup(
            string szGroupName,
            bool bEnableSound,
            bool bEnableHistory,
            bool bEnableDiskLogging,
            PopupStyle sPopUpStyle,
            int lLocalSoundRepeat,
            string szAccess_Name)
        {
            SCADAlarmDataSet.GroupRow groupRow = scadalarmDataSet.Group.NewGroupRow();
            groupRow.szGroupName = szGroupName;
            groupRow.bEnableSound = bEnableSound;
            groupRow.bEnableHistory = bEnableHistory;
            groupRow.bEnableDiskLogging = bEnableDiskLogging;
            groupRow.sPopUpStyle = (byte)sPopUpStyle;
            groupRow.lLocalSoundRepeat = lLocalSoundRepeat;
            groupRow.szAccess_Name = szAccess_Name;
            scadalarmDataSet.Group.Rows.Add(groupRow);
        }

        public void AddNameList(
            string szGroupName,
            string szUserName)
        {
            SCADAlarmDataSet.NameListRow nameListRow = scadalarmDataSet.NameList.NewNameListRow();
            nameListRow.szGroupName = szGroupName;
            nameListRow.szUserName = szUserName;
            scadalarmDataSet.NameList.Rows.Add(nameListRow);
        }

        public SCADAlarmDataSet.TagnameRow NewTagname() { return scadalarmDataSet.Tagname.NewTagnameRow(); }

        public void AddTagname(SCADAlarmDataSet.TagnameRow row) { scadalarmDataSet.Tagname.Rows.Add(row); }

        public SCADAlarmDataSet.AnalogRow NewAnalog() { return scadalarmDataSet.Analog.NewAnalogRow(); }

        public void AddAnalog(SCADAlarmDataSet.AnalogRow row) { scadalarmDataSet.Analog.Rows.Add(row); }

        public SCADAlarmDataSet._Digital__Bitpick_Row NewDigital() { return scadalarmDataSet._Digital__Bitpick_.New_Digital__Bitpick_Row(); }

        public void AddDigital(SCADAlarmDataSet._Digital__Bitpick_Row row) { scadalarmDataSet._Digital__Bitpick_.Rows.Add(row); }

        // Create a blank database and write the DataSet records to it.
        public void Commit()
        {
            // The order here is critical due to table relationships.
            //
            // Group records must specify an accessName contained in DDE, so DDE must be written before Group.
            // PhoneNumber records must specify a userName contained in PhoneName, so PhoneName must be written before PhoneNumber.
            // NameList records must specify a groupName contained in Group, so Group must be written before NameList.
            // NameList records must specify a userName contained in PhoneName, so PhoneName must be written before NameList.
            // Source Types must specify an accessName contained in DDE, so DDE must be written before Source Types. 
            // Tagname records must specify a groupName contain in Group, so Group must be written before Tagname.
            // Analog records must specify an accessName contained in DDE, so DDE must be written before Analog.
            // Analog records must specify a tagName contained in Tagname, so Tagname must be written before Analog.
            //
            // The PhoneName table has no dependencies.
            // The DDE table has no dependencies.
            // The Group table is dependent on the DDE table.
            // The PhoneNumber table is dependent on the PhoneName table.
            // The NameList table is dependent on the PhoneName table and the Group table.
            // The Source Types table is dependent on the DDE table.
            // The Tagname table is dependent on the Group table.
            // The Analog table is dependent on the DDE table and the Tagname table.
            new PhoneNameTableAdapter().Update(scadalarmDataSet.PhoneName);
            new DDETableAdapter().Update(scadalarmDataSet.DDE);
            new GroupTableAdapter().Update(scadalarmDataSet.Group);
            new PhoneNumberTableAdapter().Update(scadalarmDataSet.PhoneNumber);
            new NameListTableAdapter().Update(scadalarmDataSet.NameList);
            new Source_TypesTableAdapter().Update(scadalarmDataSet.Source_Types);
            new TagnameTableAdapter().Update(scadalarmDataSet.Tagname);
            new Digital__Bitpick_TableAdapter().Update(scadalarmDataSet._Digital__Bitpick_);
            new AnalogTableAdapter().Update(scadalarmDataSet.Analog);
            scadalarmDataSet.AcceptChanges();
        }

        public enum ConnectionType
        {
            [Description("None")]
            None,
            [Description("Alpha Pager")]
            AlphaPager,
            [Description("Voice")]
            Voice,
            [Description("Voice Pager")]
            VoicePager,
            [Description("Numeric Pager")]
            NumericPager,
            [Description("Local Alpha Pager")]
            LocalAlphaPager,
            [Description("Local Numeric Pager")]
            LocalNumericPager,
            [Description("E-Mail")]
            EMail,
            [Description("Dial-out Announcer")]
            DialoutAnnouncer,
            [Description("SMS Message")]
            SmsMessage,
            [Description("Mobile-911")]
            Mobile911
        }

        public enum PopupStyle
        {
            [Description("None")]
            None,
            [Description("Summary List on New Unacked")]
            NewUnacked,
            [Description("Summary List on Any Change")]
            AnyChange,
            [Description("Box")]
            Box
        }

        public enum TagValueType
        {
            Analog = 0,

            Bit1 = 2,
            Bit2,
            Bit3,
            Bit4,
            Bit5,
            Bit6,
            Bit7,
            Bit8,
            Bit9,
            Bit10,
            Bit11,
            Bit12,
            Bit13,
            Bit14,
            Bit15,
            Bit16,
            Bit17,
            Bit18,
            Bit19,
            Bit20,
            Bit21,
            Bit22,
            Bit23,
            Bit24,
            Bit25,
            Bit26,
            Bit27,
            Bit28,
            Bit29,
            Bit30,
            Bit31,
            Bit32,
            Text = 34,
            RemoteAlarm = 35,
            WatchDog = 100,
            Filter = 101
        }

        public enum AnalogAlarmType
        {
            HIHI,
            HI,
            LO,
            LOLO
        }

        public enum Scaling
        {
            None,
            Linear,
            SquareRoot,
            BitMask,
            RemoteAlarm
        }

        public enum Priority
        {
            High = 0,
            Medium = 100,
            Low = 200
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
    }
}
