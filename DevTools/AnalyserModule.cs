using System;
using System.Collections.Generic;
using System.Text;
using XMPMS.Interfaces;
using System.Threading;
using System.Windows.Forms;
using XMPMS.Core;

namespace DevTools
{
    public class AnalyserModule : IMasterServerModule
    {
        private Thread analyserThread;

        private frmAnalyzer analyserForm;

        public bool AutoLoad
        {
            get { return true; }
        }

        public void Initialise(MasterServer masterServer)
        {
            analyserThread = new Thread(new ThreadStart(Run));
            analyserThread.Start();
        }

        public void Shutdown()
        {
            if (analyserForm != null)
            {
                analyserForm.Close();
            }

            if (analyserThread != null && analyserThread.IsAlive)
            {
                analyserThread.Abort();
                analyserThread.Join();
                analyserThread = null;
            }
        }

        private void Run()
        {
            analyserForm = new frmAnalyzer();
            Application.Run(analyserForm);
            analyserThread = null;
            analyserForm = null;
        }
    }
}
