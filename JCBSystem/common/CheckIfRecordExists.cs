using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.common
{
    public class CheckIfRecordExists
    {
        private readonly string connectionString;

        public CheckIfRecordExists()
        {
            this.connectionString = DatabaseConfig.ConnectionString;
        }
        public async Task<bool> CheckIfRecordExistsAsync(List<object> filter, string tableName, string whereCondition)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(whereCondition))
                throw new ArgumentException("Table name and where condition must not be null or empty.");

            // Use a constant format for the query to reduce the risk of dynamic SQL issues
            const string queryFormat = "SELECT COUNT(1) FROM [{0}] WHERE {1}";

            string query = string.Format(queryFormat, tableName, whereCondition);
            int index = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (filter?.Count > 0)
                    {
                        foreach (var param in filter)
                        {
                            string paramName = "@param" + index;
                            command.Parameters.AddWithValue(paramName, param ?? DBNull.Value);
                            index++;
                        }
                    }

                    int recordCount = (int)await command.ExecuteScalarAsync();
                    return recordCount > 0;
                }
            }
        }

    }
}
