using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.common
{
    public class GenerateNextValues
    {
        private readonly string connectionString;

        public GenerateNextValues(string connectionString)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<string> GenerateNextIdAsync(string tableName, string primaryKey, string prefix)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(primaryKey) || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Table name, primary key, and prefix must not be null or empty.");

            const string queryFormat = "SELECT TOP 1 [{0}] FROM [{1}] ORDER BY [{0}] DESC"; // Define constant format for the query

            string lastId = null;

            // Construct the query using the constant format
            string query = string.Format(queryFormat, primaryKey, tableName);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Execute the query and get the last inserted ID
                    var result = await command.ExecuteScalarAsync();
                    lastId = result != null ? result.ToString() : null;
                }
            }

            int nextNumber = 1; // Default to "CS000001" if no records exist

            if (lastId != null)
            {
                // Extract the numeric part of the last ID
                var numberPart = lastId.Substring(prefix.Length); // Remove the prefix

                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1; // Increment the number
                }
            }

            // Format the new ID as "PREFIXXXXXX"
            return $"{prefix}{nextNumber:D6}"; // D6 means pad with leading zeros to ensure 6 digits
        }



        public async Task<(string WithPrefix, long WithoutPrefix)>
            GenerateNextNumberAsync(List<object> filter, string tableName, string key, string whereCondition, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(whereCondition))
                throw new ArgumentException("Table name, key, and where condition must not be null or empty.");

            string lastId = null;
            int index = 0;

            // Constant for the SQL query format to improve consistency and avoid duplication
            const string queryFormat = @"SELECT TOP 1 [{0}] FROM [{1}] WHERE {2} ORDER BY [{0}] DESC";

            // Construct the query using the constant query format
            string query = string.Format(queryFormat, key, tableName, whereCondition);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (filter.Count > 0)
                    {
                        foreach (var param in filter)
                        {
                            string paramName = "@param" + index;
                            command.Parameters.AddWithValue(paramName, param);
                            index++;
                        }
                    }

                    // Execute the query and get the last inserted ID
                    var result = await command.ExecuteScalarAsync();
                    lastId = result != null ? result.ToString() : null;
                }
            }

            long nextNumber = 1; // Default to 1 if no record is found

            if (!string.IsNullOrEmpty(lastId))
            {
                string numberPart;

                if (!string.IsNullOrEmpty(prefix) && lastId.StartsWith(prefix))
                {
                    // If prefix exists, remove it from the lastId
                    numberPart = lastId.Substring(prefix.Length);
                }
                else
                {
                    // If no prefix, consider the entire lastId as the numeric part
                    numberPart = lastId;
                }

                // Try to parse the numeric part
                if (long.TryParse(numberPart, out long lastNumber))
                {
                    nextNumber = lastNumber + 1; // Increment the number
                }
            }

            // Format the new ID with or without the prefix
            string withPrefix = !string.IsNullOrEmpty(prefix) ? $"{prefix}{nextNumber}" : $"{nextNumber}";

            return (withPrefix, nextNumber);
        }

    }
}
