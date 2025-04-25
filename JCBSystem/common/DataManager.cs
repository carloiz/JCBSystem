using JCBSystem.Connection;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.common
{
    public class DataManager
    {

        private readonly IDbConnectionFactory _connectionFactory;


        private readonly Color headerForeColor = Color.White;
        private readonly Color headerBackColor = Color.FromArgb(64, 64, 64);

        private readonly string dateFormat = "dddd, MMMM dd, yyyy hh:mm tt";

        public DataManager() 
        {
            this._connectionFactory = ConnectionFactorySelector.GetFactory();
        }    

        public async Task<(string, int)>
          SearchWithPaginatedAsync<T>(
              List<object> filter,
              string countQuery,
              string dataQuery,
              DataGridView dataGrid,
              List<string> imageColumns,
              Dictionary<string, string> customColumnHeaders, // Bagong parameter para sa custom headers
              int pageNumber = 1,
              int pageSize = 10
          )
          where T : new()
        {
            int index = 0;
            var resultList = new List<T>();

            // Calculate offset for pagination
            int offset = (pageNumber - 1) * pageSize;
            int totalRecords = 0;

            using (var connection = _connectionFactory.CreateConnection())
            {
                await OpenConnectionAsync(connection);

                bool isOdbc = connection is OdbcConnection;

                string finalCountQuery = Modules.ReplaceSharpWithParams(countQuery, isOdbc);
                string finalDataQuery = Modules.ReplaceSharpWithParams(dataQuery, isOdbc);

                if (!string.IsNullOrEmpty(finalCountQuery))
                {
                    // Execute the count query to get total records
                    using (var countCommand = connection.CreateCommand())
                    {
                        countCommand.CommandText = finalCountQuery;

                        if (filter.Count > 0)
                        {
                            foreach (var param in filter)
                            {
                                string paramName = "@param" + index;

                                var parameter = countCommand.CreateParameter();
                                parameter.ParameterName = paramName;
                                parameter.Value = param;
                                countCommand.Parameters.Add(parameter);

                                index++;
                            }
                        }

                        // Execute the count query to get total records
                        if (countCommand is DbCommand dbCountCommand)
                        {
                            var result = await dbCountCommand.ExecuteScalarAsync();
                            totalRecords = Convert.ToInt32(result);
                        }
                        else
                        {
                            var result = countCommand.ExecuteScalar();
                            totalRecords = Convert.ToInt32(result);
                        }
                    }

                }

                // Execute the data query to get paginated records
                using (var command = connection.CreateCommand())
                {

                    // Explicitly create parameters for pagination
                    var offsetParameter = command.CreateParameter();
                    offsetParameter.ParameterName = "@Offset";
                    offsetParameter.Value = offset;
                    command.Parameters.Add(offsetParameter);

                    var pageSizeParameter = command.CreateParameter();
                    pageSizeParameter.ParameterName = "@PageSize";
                    pageSizeParameter.Value = pageSize;
                    command.Parameters.Add(pageSizeParameter);

                    command.CommandText = finalDataQuery;

                    index = 0;

                    if (filter.Count > 0)
                    {
                        foreach (var param in filter)
                        {
                            string paramName = "@param" + index;

                            var parameter = command.CreateParameter();
                            parameter.ParameterName = paramName;
                            parameter.Value = param;
                            command.Parameters.Add(parameter);

                            index++;
                        }
                    }

                    // Cast to DbCommand to access ExecuteReaderAsync
                    if (command is DbCommand dbCommand)
                    {
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            // Reading paginated data
                            while (await reader.ReadAsync())
                            {
                                T entity = new T();
                                foreach (var prop in typeof(T).GetProperties())
                                {
                                    var columnName = prop.Name;
                                    if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                                    {
                                        var columnValue = reader[columnName];

                                        // Check if the property is an enum
                                        if (prop.PropertyType.IsEnum)
                                        {
                                            if (int.TryParse(columnValue.ToString(), out int enumValue))
                                            {
                                                prop.SetValue(entity, Enum.ToObject(prop.PropertyType, enumValue));
                                            }
                                        }
                                        // Check if the property is a boolean (bool)
                                        else if (prop.Name == "Status")
                                        {
                                            // Convert bool to string ("Active" or "Inactive") specifically for the "Status" property
                                            bool boolValue = Convert.ToBoolean(columnValue);
                                            string displayValue = boolValue ? "Active" : "Inactive";
                                            prop.SetValue(entity, displayValue);
                                        }

                                        else if (columnValue != null && prop.PropertyType.IsAssignableFrom(columnValue.GetType()))
                                        {
                                            prop.SetValue(entity, columnValue);
                                        }
                                        else if (prop.PropertyType == typeof(string))
                                        {
                                            prop.SetValue(entity, columnValue.ToString());
                                        }
                                        else if (prop.PropertyType == typeof(int) && columnValue is string)
                                        {
                                            prop.SetValue(entity, int.Parse(columnValue.ToString()));
                                        }
                                    }
                                }
                                resultList.Add(entity);
                            }
                        }
                    }
                }
            }

            if (resultList == null || !resultList.Any())
            {
                dataGrid.DataSource = null;
                return ($"No data found in the result.", 0);
            }

            // Bind data to DataGridView
            dataGrid.DataSource = resultList;
            dataGrid.RowHeadersVisible = false;
            dataGrid.EnableHeadersVisualStyles = false;
            dataGrid.ColumnHeadersDefaultCellStyle.BackColor = headerBackColor;
            dataGrid.ColumnHeadersDefaultCellStyle.ForeColor = headerForeColor;
            dataGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Regular);
            dataGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 5, 5, 5);

            dataGrid.CellFormatting += (sender, e) =>
            {
                // Check if the value in the cell is a DateTime
                if (e.Value is DateTime dateValue)
                {
                    // Format the DateTime for display
                    e.Value = dateValue.ToString(dateFormat, CultureInfo.InvariantCulture);
                    e.FormattingApplied = true;
                }
            };

            // Center-align header text
            dataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Center-align cell content for each column
            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Set AutoSizeColumnsMode to Fill to evenly distribute the column width
            dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Apply custom column headers
            if (customColumnHeaders != null)
            {
                foreach (var column in dataGrid.Columns.Cast<DataGridViewColumn>())
                {
                    if (customColumnHeaders.ContainsKey(column.Name))
                    {
                        column.HeaderText = customColumnHeaders[column.Name];
                    }
                }
            }

            // Exclude image columns from AutoSizeColumnsMode.Fill
            if (imageColumns != null && imageColumns.Count > 0)
            {
                foreach (string imageColumnName in imageColumns)
                {
                    if (dataGrid.Columns.Contains(imageColumnName))
                    {
                        // Set a fixed width for the image columns
                        dataGrid.Columns[imageColumnName].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dataGrid.Columns[imageColumnName].Width = 35; // Set your desired fixed width for image columns
                        dataGrid.Columns[imageColumnName].DisplayIndex = dataGrid.Columns.Count - 1; // Optional: move to the last position
                    }
                }
            }

            // **NEW: Enable multiline support and row auto-sizing**
            dataGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;


            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            // Return result with additional information if necessary
            return (string.Empty, totalRecords);


        }



        // SELECT ALL
        public async Task<(string, int)>
            SelectAllWithPaginatedAsync<T>(
                string countQuery,
                string dataQuery,
                DataGridView dataGrid,
                List<string> imageColumns,
                Dictionary<string, string> customColumnHeaders, // Bagong parameter para sa custom headers
                int pageNumber = 1,
                int pageSize = 10
            )
            where T : new()
        {
            var resultList = new List<T>();

            // Calculate offset for pagination
            int offset = (pageNumber - 1) * pageSize;
            int totalRecords = 0;

            using (var connection = _connectionFactory.CreateConnection())
            {
                await OpenConnectionAsync(connection);


                bool isOdbc = connection is OdbcConnection;

                string finalCountQuery = Modules.ReplaceSharpWithParams(countQuery, isOdbc);
                string finalDataQuery = Modules.ReplaceSharpWithParams(dataQuery, isOdbc);


                if (!string.IsNullOrEmpty(finalCountQuery))
                {
                    // Execute the count query to get total records
                    using (var countCommand = connection.CreateCommand())
                    {
                        countCommand.CommandText = finalCountQuery;

                        // Gamitin dynamic kung supported ang async, fallback to sync kung hindi
                        if (countCommand is DbCommand dbCountCommand)
                        {
                            var result = await dbCountCommand.ExecuteScalarAsync();
                            totalRecords = Convert.ToInt32(result);
                        }
                        else
                        {
                            var result = countCommand.ExecuteScalar();
                            totalRecords = Convert.ToInt32(result);
                        }
                    }

                }

                // Execute the data query to get paginated records
                // Execute the count query to get total records
                using (var command = connection.CreateCommand())
                {

                    // Explicitly create parameters for pagination
                    var offsetParameter = command.CreateParameter();
                    offsetParameter.ParameterName = "@Offset";
                    offsetParameter.Value = offset;
                    command.Parameters.Add(offsetParameter);

                    var pageSizeParameter = command.CreateParameter();
                    pageSizeParameter.ParameterName = "@PageSize";
                    pageSizeParameter.Value = pageSize;
                    command.Parameters.Add(pageSizeParameter);

                    command.CommandText = finalDataQuery;

                    // Cast to DbCommand to access ExecuteReaderAsync
                    if (command is DbCommand dbCommand)
                    {
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            // Reading paginated data
                            while (await reader.ReadAsync())
                            {
                                T entity = new T();
                                foreach (var prop in typeof(T).GetProperties())
                                {
                                    var columnName = prop.Name;
                                    if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                                    {
                                        var columnValue = reader[columnName];

                                        // Check if the property is an enum
                                        if (prop.PropertyType.IsEnum)
                                        {
                                            if (int.TryParse(columnValue.ToString(), out int enumValue))
                                            {
                                                prop.SetValue(entity, Enum.ToObject(prop.PropertyType, enumValue));
                                            }
                                        }
                                        // Check if the property is a boolean (bool)
                                        else if (prop.Name == "Status")
                                        {
                                            // Convert bool to string ("Active" or "Inactive") specifically for the "Status" property
                                            bool boolValue = Convert.ToBoolean(columnValue);
                                            string displayValue = boolValue ? "Active" : "Inactive";
                                            prop.SetValue(entity, displayValue);
                                        }
                                        // Handle other types
                                        else if (columnValue != null && prop.PropertyType.IsAssignableFrom(columnValue.GetType()))
                                        {
                                            prop.SetValue(entity, columnValue);
                                        }
                                        else if (prop.PropertyType == typeof(string))
                                        {
                                            prop.SetValue(entity, columnValue.ToString());
                                        }
                                        else if (prop.PropertyType == typeof(int) && columnValue is string)
                                        {
                                            prop.SetValue(entity, int.Parse(columnValue.ToString()));
                                        }
                                    }
                                }
                                resultList.Add(entity);
                            }
                        }
                    }
                }
            }

            if (resultList == null || !resultList.Any())
            {
                dataGrid.DataSource = null;
                return ($"No data found in the result.", 0);
            }

            // Bind data to DataGridView
            dataGrid.DataSource = resultList;
            dataGrid.RowHeadersVisible = false;
            dataGrid.EnableHeadersVisualStyles = false;
            dataGrid.ColumnHeadersDefaultCellStyle.BackColor = headerBackColor;
            dataGrid.ColumnHeadersDefaultCellStyle.ForeColor = headerForeColor;
            dataGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Regular);
            dataGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 5, 5, 5);


            dataGrid.CellFormatting += (sender, e) =>
            {
                // Check if the value in the cell is a DateTime
                if (e.Value is DateTime dateValue)
                {
                    // Format the DateTime for display
                    e.Value = dateValue.ToString(dateFormat, CultureInfo.InvariantCulture);
                    e.FormattingApplied = true;
                }
            };

            // Center-align header text
            dataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Center-align cell content for each column
            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Set AutoSizeColumnsMode to Fill to evenly distribute the column width
            dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Apply custom column headers
            if (customColumnHeaders != null)
            {
                foreach (var column in dataGrid.Columns.Cast<DataGridViewColumn>())
                {
                    if (customColumnHeaders.ContainsKey(column.Name))
                    {
                        column.HeaderText = customColumnHeaders[column.Name];
                    }
                }
            }

            // Exclude image columns from AutoSizeColumnsMode.Fill
            if (imageColumns != null && imageColumns.Count > 0)
            {
                foreach (string imageColumnName in imageColumns)
                {
                    if (dataGrid.Columns.Contains(imageColumnName))
                    {
                        // Set a fixed width for the image columns
                        dataGrid.Columns[imageColumnName].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dataGrid.Columns[imageColumnName].Width = 35; // Set your desired fixed width for image columns
                        dataGrid.Columns[imageColumnName].DisplayIndex = dataGrid.Columns.Count - 1; // Optional: move to the last position
                    }
                }
            }

            // **NEW: Enable multiline support and row auto-sizing**
            dataGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            // Return result with additional information if necessary
            return (string.Empty, totalRecords);
        }




        // INSERT
        public async Task<object> InsertAsync<T>(
          T entity,
          string tableName,
          IDbConnection connection,
          IDbTransaction transaction)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid table name.", nameof(tableName));

            try
            {
                var properties = typeof(T).GetProperties()
                                          .Where(p => p.CanRead && p.GetValue(entity) != null)
                                          .ToArray();

                if (!properties.Any())
                    throw new ArgumentException("Entity has no readable properties with values.");

                bool isOdbc = connection is OdbcConnection;
                if (isOdbc)
                    tableName.ToLower();

                string columnList = string.Join(", ", properties.Select(p =>
                    isOdbc ? $"`{p.Name}`" : $"[{p.Name}]"
                ));

                string valueList = string.Join(", ", properties.Select(p =>
                    isOdbc ? "?" : "@" + p.Name
                ));

                string insertQuery = $"INSERT INTO {(isOdbc ? $"`{tableName}`" : $"[{tableName}]")} ({columnList}) VALUES ({valueList});";

                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = insertQuery;

                    foreach (var prop in properties)
                    {
                        var param = insertCommand.CreateParameter();
                        param.ParameterName = isOdbc ? null : "@" + prop.Name;
                        param.Value = prop.GetValue(entity) ?? DBNull.Value;
                        insertCommand.Parameters.Add(param);
                    }

                    // 🔁 First, execute the INSERT
                    if (insertCommand is DbCommand insertDbCommand)
                        await insertDbCommand.ExecuteNonQueryAsync();
                    else
                        insertCommand.ExecuteNonQuery();

                    // 🔁 Then, get the last inserted ID
                    using (var identityCommand = connection.CreateCommand())
                    {
                        identityCommand.Transaction = transaction;
                        identityCommand.CommandText = isOdbc ? "SELECT LAST_INSERT_ID();" : "SELECT SCOPE_IDENTITY();";

                        if (identityCommand is DbCommand identityDbCommand)
                            return await identityDbCommand.ExecuteScalarAsync();
                        else
                            return identityCommand.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while inserting data: " + ex.Message, ex);
            }
        }







        public async Task<int> UpdateAsync<T>(
           T entity,
           string tableName,
           IDbConnection connection,
           IDbTransaction transaction,
           string primaryKey = null,
           string whereCondition = null,
           List<object> additionalParameters = null)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid table name.", nameof(tableName));

            try
            {
                var isOdbc = connection is OdbcConnection;

                if (isOdbc)
                    tableName.ToLower();

                var properties = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();

                var setProperties = string.IsNullOrEmpty(primaryKey)
                    ? properties
                    : properties.Where(p => !string.Equals(p.Name, primaryKey, StringComparison.OrdinalIgnoreCase)).ToArray();

                // SET clause
                var setClause = string.Join(", ", setProperties.Select(p =>
                    isOdbc ? $"`{p.Name}` = ?" : $"[{p.Name}] = @{p.Name}"
                ));

                string query;

                if (!string.IsNullOrEmpty(primaryKey))
                {
                    query = isOdbc
                        ? $"UPDATE `{tableName}` SET {setClause} WHERE `{primaryKey}` = ?"
                        : $"UPDATE [{tableName}] SET {setClause} WHERE [{primaryKey}] = @{primaryKey}";
                }
                else if (!string.IsNullOrEmpty(whereCondition))
                {
                    string finalQuery = Modules.ReplaceSharpWithParams(whereCondition, isOdbc);

                    query = isOdbc
                        ? $"UPDATE `{tableName}` SET {setClause} WHERE {finalQuery}"
                        : $"UPDATE [{tableName}] SET {setClause} WHERE {finalQuery}";
                }
                else
                {
                    query = isOdbc
                        ? $"UPDATE `{tableName}` SET {setClause}"
                        : $"UPDATE [{tableName}] SET {setClause}";
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = query;

                    // Parameter index tracking for ODBC (uses '?')
                    int paramIndex = 0;

                    // SET parameters
                    foreach (var prop in setProperties)
                    {
                        var param = command.CreateParameter();
                        param.ParameterName = isOdbc ? null : "@" + prop.Name;
                        param.Value = prop.GetValue(entity) ?? DBNull.Value;
                        command.Parameters.Add(param);
                        paramIndex++;
                    }

                    // Primary key parameter
                    if (!string.IsNullOrEmpty(primaryKey))
                    {
                        var pkProp = typeof(T).GetProperty(primaryKey);
                        if (pkProp != null)
                        {
                            var pkParam = command.CreateParameter();
                            pkParam.ParameterName = isOdbc ? null : "@" + primaryKey;
                            pkParam.Value = pkProp.GetValue(entity) ?? DBNull.Value;
                            command.Parameters.Add(pkParam);
                            paramIndex++;
                        }
                    }

                    // Additional parameters (for WHERE clause)
                    if (additionalParameters != null)
                    {
                        foreach (var val in additionalParameters)
                        {
                            var param = command.CreateParameter();
                            param.ParameterName = isOdbc ? null : $"@param{paramIndex}";
                            param.Value = val ?? DBNull.Value;
                            command.Parameters.Add(param);
                            paramIndex++;
                        }
                    }

                    // Execute async if supported
                    if (command is DbCommand dbCommand)
                        return await dbCommand.ExecuteNonQueryAsync();
                    else
                        return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating data: " + ex.Message, ex);
            }
        }





        public async Task<int> DeleteAsync(
            List<object> filterValues,
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            string whereConditions = null)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid table name.", nameof(tableName));

            if (filterValues.Count > 0 && string.IsNullOrWhiteSpace(whereConditions))
                throw new ArgumentException("WHERE conditions are required when filters are provided.");

            var isOdbc = connection is OdbcConnection;

            string finalQuery = Modules.ReplaceSharpWithParams(whereConditions, isOdbc);

            if (isOdbc)
                tableName.ToLower();

            // 🔧 Build the DELETE query
            string query = string.IsNullOrWhiteSpace(whereConditions)
                ? (isOdbc ? $"DELETE FROM `{tableName}`" : $"DELETE FROM [{tableName}]")
                : (isOdbc ? $"DELETE FROM `{tableName}` WHERE {finalQuery}" : $"DELETE FROM [{tableName}] WHERE {finalQuery}");

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = query;

                    // 🧷 Bind parameters (ODBC uses `?`, SQL Server uses `@paramX`)
                    for (int i = 0; i < filterValues.Count; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = isOdbc ? null : $"@param{i}";
                        parameter.Value = filterValues[i] ?? DBNull.Value;
                        command.Parameters.Add(parameter);
                    }

                    // ✅ Execute async if supported
                    if (command is DbCommand dbCommand)
                        return await dbCommand.ExecuteNonQueryAsync();
                    else
                        return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while deleting data: " + ex.Message, ex);
            }
        }






        public async Task CommitAndRollbackMethod(Func<IDbConnection, IDbTransaction, Task> action)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                await OpenConnectionAsync(connection);

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await action(connection, transaction);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // rollback on error
                        MessageBox.Show(
                            $"{ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }

        private async Task OpenConnectionAsync(IDbConnection connection)
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
