using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Connection
{
    public class OdbcConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public OdbcConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new OdbcConnection(_connectionString);
        }
    }
}
