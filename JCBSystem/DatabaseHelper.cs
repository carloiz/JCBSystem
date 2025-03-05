using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem
{
    public class DatabaseHelper
    {


        // Method to create the Crystal Reports connection
        public ConnectionInfo crystalConnection(string connectionString)
        {
            var crConnectionInfo = new ConnectionInfo();

            try
            {
                // CRYSTAL REPORT CONNECTION
                crConnectionInfo.ServerName = connectionString;
                crConnectionInfo.IntegratedSecurity = true; // Using Windows Authentication
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error CRYSTAL REPORT Connection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                // Close your form or handle as needed
                Application.Exit();
            }

            return crConnectionInfo;
        }


    }
}
