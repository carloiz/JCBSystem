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
using Npgsql;

namespace JCBSystem.Connection
{
    public static class ConnectionFactorySelector
    {
        private static readonly string _connName = "odbc";

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
                case "npgsql":
                    return new NpgsqlConnectionFactory(connectionString);
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
            
            if (command is NpgsqlCommand npgSqlCmd)
                return new NpgsqlDataAdapter(npgSqlCmd);
            // Add more if needed (e.g., MySqlCommand)

            throw new NotSupportedException($"Unsupported command type: {command.GetType().Name}");
        }
    }



}
