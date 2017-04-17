using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryWarden
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Mutex appMutex;

        public App()
        {
            Window messageBoxLeash = new Window();
            const string mutexName = "Mutex - Memory Warden by Kristofer Christakos {5087A53A-687B-4A2D-A125-3E46E3B8CECF}";
            try
            {
                appMutex = Mutex.OpenExisting(mutexName);
                MessageBox.Show(messageBoxLeash,
                    "Memory Warden is already runing.\n" +
                    "Look for the tray icon in the bottom right.",
                    "Memory Warden Error",
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                appMutex.ReleaseMutex();
                appMutex.Dispose();
                Application.Current.Shutdown();
                return;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                //The named mutex does not exist.
                //Create the mutex
                appMutex = new Mutex(true, mutexName);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                //The named mutex exists, but the user does not have the security access required to use it.
                MessageBox.Show(messageBoxLeash,
                    "Memory Warden is already runing.\n" +
                    "Look for the tray icon in the bottom right.",
                    "Memory Warden Error",
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current.Shutdown();
                return;
            }
            catch (IOException e)
            {
                MessageBox.Show(messageBoxLeash,
                    "Error while trying to determine if another instance\n" +
                    "of Memory Warden is already running.\n\n" +
                    e.Message,
                    "Memory Warden Error",
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current.Shutdown();
                return;
            }
        }
    }
}
