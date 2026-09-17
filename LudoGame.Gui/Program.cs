using System;
using System.Windows.Forms;

namespace LudoGame.Gui
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.ThreadException += (_, e) =>
            {
                try { System.IO.File.WriteAllText(@"d:\Test 2\Wenura\crash.log", e.Exception.ToString()); } catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                try { System.IO.File.WriteAllText(@"d:\Test 2\Wenura\crash.log", e.ExceptionObject.ToString()); } catch { }
            };

            try 
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try { System.IO.File.WriteAllText(@"d:\Test 2\Wenura\crash.log", ex.ToString()); } catch { }
            }
        }
    }
}