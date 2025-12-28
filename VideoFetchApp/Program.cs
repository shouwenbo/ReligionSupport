using System.Net;

namespace VideoFetchApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 强制启用 TLS 1.2 / 1.3
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12
                | (SecurityProtocolType)12288; // TLS 1.3，枚举值硬编码


            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}