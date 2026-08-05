using System;
using System.Windows.Forms;

namespace LudoGame.Gui
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try 
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Console.WriteLine("CRITICAL CRASH: " + ex.ToString());
            }
        }
    }
}