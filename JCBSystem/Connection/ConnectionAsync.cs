using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Connection
{
    public class ConnectionAsync
    {
        public async Task OpenConnectionAsync(IDbConnection connection)
        {
            // IDbConnection does not have OpenAsync, so cast only if supported
            if (connection is SqlConnection sqlConn)
                await sqlConn.OpenAsync();
            else if (connection is MySqlConnection mySqlConn)
                await mySqlConn.OpenAsync();
            else
                connection.Open(); // fallback for ODBC or others
        }
    }
}
