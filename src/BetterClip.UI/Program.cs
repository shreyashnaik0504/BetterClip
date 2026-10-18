#nullable enable
using System;
using System.Windows;

namespace BetterClip.UI
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Application();
            var window = new MainWindow();
            app.Run(window);
        }
    }
}