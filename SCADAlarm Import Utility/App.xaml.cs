using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SCADAlarm_Import_Utility.ViewModel;

namespace SCADAlarm_Import_Utility
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs sea)
        {
            MainWindow wnd = new MainWindow();

            if (sea.Args.Length == 1)
            {
                try
                {
                    MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
                    mainWindowViewModel.SilentLoadSCADAlarmFile(sea.Args[0]);
                    mainWindowViewModel.SilentSaveWIN911File();
                }
                catch(Exception)
                {
                }
                finally
                {
                    this.Shutdown();
                }
            }
            else wnd.Show();
        }
    }
}
