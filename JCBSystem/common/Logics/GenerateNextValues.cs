using JCBSystem.Connection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.common
{
    public class GenerateNextValues
    {

        private readonly ConnectionAsync async = new ConnectionAsync();

        private readonly IDbConnectionFactory _connectionFactory;

        public GenerateNextValues()
        {
            this._connectionFactory = ConnectionFactorySelector.GetFactory();
        }

        public async Task<string> GenerateNextIdAsync(string tableName, string primaryKey, string prefix)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(primaryKey) || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Table name, primary key, and prefix must not be null or empty.");

            string lastId = null;

            using (var connection = _connectionFactory.CreateConnection())
            {
                await async.OpenConnectionAsync(connection);

                bool isOdbc =  connection is OdbcConnection || connection is NpgsqlConnection;

                // Use correct SQL format depending on database type
                string query = isOdbc
                    ? $"SELECT {primaryKey} FROM {tableName} ORDER BY {primaryKey} DESC LIMIT 1"
                    : $"SELECT TOP 1 [{primaryKey}] FROM [{tableName}] ORDER BY [{primaryKey}] DESC";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    if (command is DbCommand dbCommand)
                    {
                        var result = await dbCommand.ExecuteScalarAsync();
                        lastId = result != null ? result.ToString() : null;
                    }
                }
            }

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastId))
            {
                var numberPart = lastId.Substring(prefix.Length);

                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D6}";
        }




        public async Task<(string WithPrefix, long WithoutPrefix)> GenerateNextNumberAsync(
              List<object> filter,
              string tableName,
              string key,
              string whereCondition,
              string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(whereCondition))
                throw new ArgumentException("Table name, key, and where condition must not be null or empty.");

            string lastId = null;
            int index = 0;

            using (var connection = _connectionFactory.CreateConnection())
            {
                await async.OpenConnectionAsync(connection);

                bool isOdbc = connection is OdbcConnection;

                bool isNpgSql = connection is NpgsqlConnection;

                // Format query depending on connection type
                string query = isOdbc || isNpgSql

                    ? $"SELECT {key} FROM {tableName} WHERE {whereCondition} ORDER BY {key} DESC LIMIT 1"
                    : $"SELECT TOP 1 [{key}] FROM [{tableName}] WHERE {whereCondition} ORDER BY [{key}] DESC";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    if (filter?.Count > 0)
                    {
                        foreach (var param in filter)
                        {
                            string paramName = isOdbc ? "?" : "@param" + index;

                            var dbParam = command.CreateParameter();
                            dbParam.ParameterName = paramName;
                            dbParam.Value = param ?? DBNull.Value;

                            command.Parameters.Add(dbParam);
                            index++;
                        }
                    }
                    if (command is DbCommand dbCommand)
                    {
                        var result = await dbCommand.ExecuteScalarAsync();
                        lastId = result?.ToString();
                    }
                }
            }

            long nextNumber = 1;

            if (!string.IsNullOrEmpty(lastId))
            {
                string numberPart = !string.IsNullOrEmpty(prefix) && lastId.StartsWith(prefix)
                    ? lastId.Substring(prefix.Length)
                    : lastId;

                if (long.TryParse(numberPart, out long lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            string withPrefix = !string.IsNullOrEmpty(prefix) ? $"{prefix}{nextNumber}" : nextNumber.ToString();

            return (withPrefix, nextNumber);
        }


    }
}
