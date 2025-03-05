using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.common
{
    public class GetFieldsValues
    {

        private readonly string connectionString;

        public GetFieldsValues()
        {
            this.connectionString = DatabaseConfig.ConnectionString;
        }

        public async Task<Dictionary<string, object>> GetFieldValuesAsync(
            List<object> filter,
            string tableName,
            List<string> fieldNamesQuery,
            List<string> fieldNames,
            string whereCondition)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(whereCondition))
                throw new ArgumentException("Table name and where condition must not be null or empty.");

            if (fieldNamesQuery == null || fieldNamesQuery.Count == 0)
                throw new ArgumentException("Field names for the query must not be null or empty.");

            if (fieldNames == null || fieldNames.Count == 0)
                throw new ArgumentException("Field names must not be null or empty.");

            // Use a constant format for the query to reduce the risk of dynamic SQL issues
            const string queryFormat = "SELECT {0} FROM [{1}] WHERE {2}";

            // Join fields for the SELECT clause
            string fields = fieldNamesQuery.Count == 1 ? fieldNamesQuery[0] : string.Join(", ", fieldNamesQuery);

            // Construct the query dynamically using the format
            string query = string.Format(queryFormat, fields, tableName, whereCondition);

            var resultDictionary = new Dictionary<string, object>();
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

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            foreach (var fieldName in fieldNames)
                            {
                                resultDictionary[fieldName] = reader[fieldName] == DBNull.Value ? null : reader[fieldName];
                            }
                        }
                    }
                }
            }

            return resultDictionary;
        }

    }
}
