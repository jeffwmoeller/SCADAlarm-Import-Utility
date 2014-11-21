using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public class SCADAlarmOperator : SCADAlarmBase
    {
        public static List<SCADAlarmOperator> Parse(string[] logicalFiles)
        {
            return ParseSections<SCADAlarmOperator>(GetSectionFile(logicalFiles, logicalFileHeader), sectionDelimiter);
        }

        private static string logicalFileHeader = "Operator File dump:";
        private static string sectionDelimiter = "#";

        override protected List<LineDelegate> LineDelegates
        {
            get
            {
                return new List<LineDelegate>{
                new LineDelegate( new ParseLineDelegate(ParseSectionHeader), sectionDelimiter),
                new LineDelegate( new ParseLineDelegate(ParseContact), contactPrefix),
                new LineDelegate( new ParseLineDelegate(ParseCallingSequence), callingSequencePrefix),
                new LineDelegate( new ParseLineDelegate(ParseGreeting), greetingPrefix),
                new LineDelegate( new ParseLineDelegate(ParseGroups), groupsPrefix) }; } }

        private static string contactPrefix = "     #";
        private static string callingSequencePrefix = "     Calling sequence in effect:";
        private static string greetingPrefix = "     Greeting:";
        private static string groupsPrefix = "     Groups:";

        public string ID { get; set; }
        public string Name { get; set; }
        public bool AdministratorAccess { get; set; }
        internal List<SCADAlarmContact> Contacts { get; set; }
        internal List<int> CallingSequence { get; set; }
        public string GreetingSpeech { get; set; }
        internal List<SCADAlarmOperatorGroup> Groups { get; set; }

        public SCADAlarmOperator()
        {
            ID = string.Empty;
            Name = string.Empty;
            AdministratorAccess = false;
            Contacts = new List<SCADAlarmContact>();
            CallingSequence = new List<int>();
            GreetingSpeech = string.Empty;
            Groups = new List<SCADAlarmOperatorGroup>();
        }

        // operator lines are of the form "#001 operator" or "#001 operator (administrator access)"
        // ID = 001, Name = "operator", AdministratorAccess is true if the optional (administrator access) is present
        private void ParseSectionHeader(string line)
        {
            // Parse the Operator's ID
            ID = line.Substring(1, 3);

            // Parse the Operator's AdministratorAccess
            AdministratorAccess = line.Contains("(administrator access)");

            // Parse the Operator's Name
            Name = AdministratorAccess ? line.Substring(5, line.Length - 27) : line.Substring(5);
        }

        // contact lines are of the form "     #1 some comment (type: accessString)." or "     #1 type: accessString."
        // ID = 1, Comment = "some comment", Type = type, AccessString = accessString
        private void ParseContact(string line)
        {
             Contacts.Add(new SCADAlarmContact(line));
        }

        // calling sequence lines are of the form "     Calling sequence in effect: #1 #2 #3 #4"
        // calling sequence lines are optional and can specify up to 4 contact IDs
        // calling sequences are dependent on the operator preference schedule in effect when the file is created
        private void ParseCallingSequence(string line)
        {
            // If the calling sequence is not empty ...
            if (line.Substring(31).Contains('#'))
            {
                // Split the calling sequence into contact IDs
                string[] contactSequence = line.Substring(34).Split('#');

                // Parse the Operator's ContactSequence
                foreach (string contactID in contactSequence) CallingSequence.Add(int.Parse(contactID.Substring(0, 1)));
            }
        }

        // greeting lines are of the form "     Greeting: greeting"
        // Greeting = "greeting"
        private void ParseGreeting(string line)
        {
            // Parse the Operator's Greeting
            GreetingSpeech = line.Substring(line.IndexOf(':') + 2);
        }

        // group lines are of the form "     Groups: group1 (4th)    group2 (1st)" or "     Groups: (none)"
        // there can be up to 4 group specified with (none) indicating no groups
        // first group { Name = "group1" Position = 4 } second group { Name = "group2" Position = 1 }
        private void ParseGroups(string line)
        {
            // if there is at least 1 group specified ...
            if (!line.Contains("(none)"))
            {
                // Split the line into group specifications
                string[] groupSpecifications = line.Substring(13).Trim().Split(new string[] { "    " }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string groupSpecification in groupSpecifications)
                {
                    // Split the group specification into group properties
                    string[] groupProperties = groupSpecification.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    // Parse the Groups properties
                    Groups.Add(new SCADAlarmOperatorGroup { Name = groupProperties[0], Position = int.Parse(groupProperties[1].Substring(1, 1)) });
                }
            }
        }
    }
}
