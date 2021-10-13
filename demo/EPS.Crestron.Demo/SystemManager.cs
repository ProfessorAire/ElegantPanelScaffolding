using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

namespace EPS.Demo
{
    public class SystemManager : IDisposable
    {
        public static SystemManager Instance { get; private set; }

        public CrestronControlSystem ControlSystem { get; private set; }

        public UI.DemoUI.Panel DemoPanel { get; private set; }

        public bool Disposed { get; private set; }

        static SystemManager()
        {
            Instance = new SystemManager();
        }

        public void Initialize(CrestronControlSystem controlSystem)
        {
            if (controlSystem == null)
            {
                throw new ArgumentNullException("controlSystem");
            }

            ControlSystem = controlSystem;
        }

        public void LoadConfig()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("SystemManager");
            }

            if (ControlSystem == null)
            {
                throw new InvalidOperationException("The SystemManager must be initialized prior to use.");
            }

            CrestronConsole.PrintLine("Loading Config.");

            this.DisposeInternals();

            this.DemoPanel = new EPS.Demo.UI.DemoUI.Panel();
            this.DemoPanel.AddPanel(new Crestron.SimplSharpPro.UI.XpanelForSmartGraphics(0x21, ControlSystem));
            this.DemoPanel.UserCodeExceptionEncountered += (o, a) => CrestronConsole.PrintLine("User Code Exception.\r\n{0}", a);
            this.DemoPanel.Register();
            this.DemoPanel.StartThreads();

            CrestronConsole.PrintLine("Config Loaded.");
        }

        public void Dispose()
        {
            this.DisposeInternals();
            Disposed = true;
        }

        private void DisposeInternals()
        {
            if (DemoPanel != null)
            {
                DemoPanel.Dispose();
                DemoPanel = null;
            }
        }
    }
}