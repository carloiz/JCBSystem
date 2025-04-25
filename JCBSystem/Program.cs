using JCBSystem.Connection;
using JCBSystem.Users;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem
{
    internal static class Program
    {
        //private static readonly string connectionString = Environment.GetEnvironmentVariable("MyDbConnection");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread] 
        static void Main()
        {

            IDbConnectionFactory _connectionFactory = ConnectionFactorySelector.GetFactory();
            
            EncryptConnectionString();


            using (var connection = _connectionFactory.CreateConnection())
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connected successfully using Windows Authentication!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Connection failed: " + ex.Message);
                    Application.Exit();
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }


        public static void EncryptConnectionString()
        {
            string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            ConfigurationSection section = config.GetSection("connectionStrings");
            if (section != null && !section.SectionInformation.IsProtected)
            {
                section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                config.Save(ConfigurationSaveMode.Modified);
                Console.WriteLine("Connection string encrypted successfully!");
            }
            else
            {
                Console.WriteLine("Connection string is already encrypted.");
            }
        }


        public static void DecryptConnectionString()
        {
            string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            ConfigurationSection section = config.GetSection("connectionStrings");
            if (section != null && section.SectionInformation.IsProtected)
            {
                section.SectionInformation.UnprotectSection();
                config.Save(ConfigurationSaveMode.Modified);
                Console.WriteLine("Connection string decrypted successfully!");
            }
            else
            {
                Console.WriteLine("Connection string is already decrypted.");
            }
        }






    }
}
