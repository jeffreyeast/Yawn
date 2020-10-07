using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yawn
{
    internal class Activity : IDisposable
    {
        DockingPanel DockingPanel;


        internal Activity(DockingPanel dockingPanel)
        {
            DockingPanel = dockingPanel;
            DockingPanel.StartActivity();
        }

        public void Dispose()
        {
            DockingPanel.StopActivity();
        }
    }
}
