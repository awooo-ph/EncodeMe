using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Windows;
using NORSU.EncodeMe.Network;
using NORSU.EncodeMe.ViewModels;

namespace NORSU.EncodeMe
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
      
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            awooo.Context = SynchronizationContext.Current;
            Client.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Client.Stop();
            base.OnExit(e);
        }
    }
}
