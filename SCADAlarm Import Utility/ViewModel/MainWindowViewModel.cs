using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SCADAlarm_Import_Utility.Model;
using System.Collections.ObjectModel;
using System.Windows;
using System.Data;

namespace SCADAlarm_Import_Utility.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            InitializeCommands();
        }

        #region Properties

        private bool saveEnabled = false;
        public bool SaveEnabled
        {
            get { return saveEnabled; }
            set { saveEnabled = value; OnPropertyChanged("SaveEnabled"); }
        }

        private string scadalarmFile = "<no file selected>";
        public string SCADAlarmFile
        {
            get { return scadalarmFile; }
            set { scadalarmFile = value; OnPropertyChanged("SCADAlarmFile"); }
        }

        private List<SCADAlarmOperator> scadalarmOperators = new List<SCADAlarmOperator>();
        public List<SCADAlarmOperator> ScadalarmOperators
        {
            get { return scadalarmOperators; }
            set { scadalarmOperators = value; OnPropertyChanged("ScadalarmOperators"); }
        }

        private List<Contact> contacts = new List<Contact>();
        public List<Contact> Contacts
        {
            get { return contacts; }
            set { contacts = value; OnPropertyChanged("Contacts"); }
        }

        private List<GroupOperator> groupOperators = new List<GroupOperator>();
        public List<GroupOperator> GroupOperators
        {
            get { return groupOperators; }
            set { groupOperators = value; OnPropertyChanged("GroupOperators"); }
        }

        private List<SCADAlarmGroup> scadalarmGroups = new List<SCADAlarmGroup>();
        public List<SCADAlarmGroup> ScadalarmGroups
        {
            get { return scadalarmGroups; }
            set { scadalarmGroups = value; OnPropertyChanged("ScadalarmGroups"); }
        }

        private List<SCADAlarmServer> scadalarmServers = new List<SCADAlarmServer>();
        public List<SCADAlarmServer> ScadalarmServers
        {
            get { return scadalarmServers; }
            set { scadalarmServers = value; OnPropertyChanged("ScadalarmServers"); }
        }

        private List<SCADAlarmTag> scadalarmTags = new List<SCADAlarmTag>();
        public List<SCADAlarmTag> ScadalarmTags
        {
            get { return scadalarmTags; }
            set { scadalarmTags = value; OnPropertyChanged("ScadalarmTags"); }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Wire up the relay commands so the view model can respond to the view.
        /// </summary>
        private void InitializeCommands()
        {
            SelectSCADAlarmFileCommand = new RelayCommand(() => SelectSCADAlarmFile());
            SaveWIN911FileCommand = new RelayCommand(() => SaveWIN911File());
        }

        public RelayCommand SelectSCADAlarmFileCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void SelectSCADAlarmFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SCADAlarm(SCADAlarm*.txt)|SCADAlarm*.txt|Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    SilentLoadSCADAlarmFile(openFileDialog.FileName);
                    SCADAlarmFile = openFileDialog.FileName;
                    SaveEnabled = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        "The selected file was not loaded due to the following error:\n\n" + e.Message,
                        "Output Failure",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );

                    SCADAlarmFile = "<no file selected>";
                    SaveEnabled = false;
                    Contacts = new List<Contact>();
                    GroupOperators = new List<GroupOperator>();
                }
            }
        }

        public RelayCommand SaveWIN911FileCommand { get; set; }

        public void SaveWIN911File()
        {
            try
            {
                SilentSaveWIN911File();
                SaveEnabled = false;
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "The output file was not saved due to the following error:\n\n" + e.Message,
                    "Output Failure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );

                SCADAlarmFile = "<no file selected>";
                Contacts = new List<Contact>();
                GroupOperators = new List<GroupOperator>();
            }
        }

        #endregion

        public void SilentLoadSCADAlarmFile(string fileName)
        {
            // Init the config from the Configuration Listing file
            SCADAlarmConfig scadalarmConfig = new SCADAlarmConfig(fileName);

            // Fetch the native SCADAlarm data
            ScadalarmOperators = scadalarmConfig.GetOperators();
            ScadalarmGroups = scadalarmConfig.GetGroups();
            ScadalarmServers = scadalarmConfig.GetServers();
            ScadalarmTags = scadalarmConfig.GetTags();

            // Create view-friendly Contacts from ScadalarmOperators.Contacts
            Contacts = new List<Contact>();
            foreach (SCADAlarmOperator scadalarmOperator in ScadalarmOperators)
            {
                Contacts.AddRange((
                    from contact in scadalarmOperator.Contacts
                    select new Contact{
                        Operator = scadalarmOperator.Name,
                        Type = SCADAlarmContact.GetEnumDescription(contact.Type),
                        AccessString = contact.AccessString,
                        // The listing file includeds a contact sequence, but this sequence
                        // depends on the defined schedules and the time at which the listing was
                        // produced.  Further, contacts are not included in the sequence if they
                        // do not appear in the schedule for the time that the listing file was
                        // created.  For these reasons, the sequence is set to the order that the
                        // contacts are defined.
                        Sequence = scadalarmOperator.Contacts.IndexOf(contact) + 1,
                        Comment = contact.Comment}).ToList());
            }

            // Create view-friendly GroupOperators from scadalarmGroups
            GroupOperators = new List<GroupOperator>();
            foreach (SCADAlarmGroup scadalarmGroup in ScadalarmGroups)
            {
                GroupOperators.AddRange((
                    from _operator in scadalarmGroup.Operators
                    orderby _operator.Position
                    select new GroupOperator
                    {
                        GroupName = scadalarmGroup.Name,
                        Operator = _operator.Operator,
                        Position = _operator.Position
                    }
                    ).ToList());
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public void SilentSaveWIN911File()
        {
            SCADAlarmToWIN911.XlateSCADAlarm(
                ScadalarmOperators,
                ScadalarmGroups,
                ScadalarmServers,
                ScadalarmTags);
        }

        public class Contact
        {
            public string Operator { get; set; }
            public string Type { get; set; }
            public string AccessString { get; set; }
            public string Comment { get; set; }
            public int Sequence { get; set; } 
        }

        public class GroupOperator
        {
            public string GroupName { get; set; }
            public string Operator { get; set; }
            public int Position { get; set; }
        }
    }
}
