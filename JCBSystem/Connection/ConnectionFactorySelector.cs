using JCBSystem.Properties;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Connection
{
    public static class ConnectionFactorySelector
    {
        private static readonly string _connName = Settings.Default.ConnectionName.ToLower();

        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MyOdbcConnection"].ConnectionString;

        public static IDbConnectionFactory GetFactory()
        {
            switch (_connName)
            {
                case "sql":
                    return new SqlConnectionFactory(connectionString);
                case "mysql":
                    return new MySqlConnectionFactory(connectionString);
                case "odbc":
                    return new OdbcConnectionFactory(connectionString);
                default:
                    throw new NotSupportedException($"Provider '{_connName}' is not supported.");
            }
        }

        public static IDataAdapter CreateDataAdapter(IDbCommand command)
        {
            if (command is SqlCommand sqlCmd)
                return new SqlDataAdapter(sqlCmd);

            if (command is OdbcCommand odbcCmd)
                return new OdbcDataAdapter(odbcCmd);

            if (command is MySqlCommand mysqlCmd)
                return new MySqlDataAdapter(mysqlCmd);
            // Add more if needed (e.g., MySqlCommand)

            throw new NotSupportedException($"Unsupported command type: {command.GetType().Name}");
        }
    }



}
