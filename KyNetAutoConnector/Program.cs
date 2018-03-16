using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace KyNetAutoConnector
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createNew;
            using (new Mutex(true, Application.ProductName, out createNew))
            {
                if (createNew)
                {
                    Application.Run(new frmMain());
                }
                else
                {
                    Thread.Sleep(1000);
                    Environment.Exit(1);
                }
            }
        }
    }
}