using System;
using System.Collections.Generic;
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
        private readonly string connectionString;

        private readonly Color headerForeColor = Color.White;
        private readonly Color headerBackColor = Color.FromArgb(64, 64, 64);

        private readonly string dateFormat = "dddd, MMMM dd, yyyy hh:mm tt";


        public DataManager(string connectionString)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
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

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                if (!string.IsNullOrEmpty(countQuery))
                {
                    // Execute the count query to get total records
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        if (filter.Count > 0)
                        {
                            foreach (var param in filter)
                            {
                                string paramName = "@param" + index;
                                countCommand.Parameters.AddWithValue(paramName, param);
                                index++;
                            }
                        }

                        totalRecords = (int)await countCommand.ExecuteScalarAsync();
                    }
                }

                // Execute the data query to get paginated records
                using (SqlCommand command = new SqlCommand(dataQuery, connection))
                {
                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    index = 0;

                    if (filter.Count > 0)
                    {
                        foreach (var param in filter)
                        {
                            string paramName = "@param" + index;
                            command.Parameters.AddWithValue(paramName, param);
                            index++;
                        }
                    }

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
                                        bool boolValue = (bool)columnValue;
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

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();


                if (!string.IsNullOrEmpty(countQuery))
                {
                    // Execute the count query to get total records
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        totalRecords = (int)await countCommand.ExecuteScalarAsync();
                    }
                }

                // Execute the data query to get paginated records
                using (SqlCommand command = new SqlCommand(dataQuery, connection))
                {
                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
                                        bool boolValue = (bool)columnValue;
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
            SqlConnection connection,
            SqlTransaction transaction
        )
        {
            // Validate table name (you can use a whitelist or stricter validation)
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
            {
                throw new ArgumentException("Invalid table name.", nameof(tableName));
            }

            try
            {
                var properties = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();

                string columns = string.Join(", ", properties.Select(p => p.Name));
                string values = string.Join(", ", properties.Select(p => "@" + p.Name));

                // Autodetect primary key column

                const string queryTemplate = "INSERT INTO [{0}] ({1}) VALUES ({2}) SELECT SCOPE_IDENTITY()";
                string query = string.Format(queryTemplate, tableName, columns, values);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Transaction = transaction;

                    foreach (var prop in properties)
                    {
                        command.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(entity) ?? DBNull.Value);
                    }

                    return await command.ExecuteScalarAsync();
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
           SqlConnection connection,
           SqlTransaction transaction,
           string primaryKey = null,
           string whereCondition = null,
           List<object> additionalParameters = null)
        {
            // Validate table name to prevent SQL injection
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
            {
                throw new ArgumentException("Invalid table name.", nameof(tableName));
            }

            try
            {
                var properties = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();
                var setProperties = string.IsNullOrEmpty(primaryKey)
                    ? properties
                    : properties.Where(p => !string.Equals(p.Name, primaryKey, StringComparison.OrdinalIgnoreCase)).ToArray();

                string setClause = string.Join(", ", setProperties.Select(p => $"{p.Name} = @{p.Name}"));

                const string queryTemplatePrimaryKey = "UPDATE [{0}] SET {1} WHERE {2} = @{2}";
                const string queryTemplateWhereCondition = "UPDATE [{0}] SET {1} WHERE {2}";
                const string queryTemplateNoCondition = "UPDATE [{0}] SET {1}";

                string query;

                if (!string.IsNullOrEmpty(primaryKey))
                {
                    query = string.Format(queryTemplatePrimaryKey, tableName, setClause, primaryKey);
                }
                else if (!string.IsNullOrEmpty(whereCondition))
                {
                    query = string.Format(queryTemplateWhereCondition, tableName, setClause, whereCondition);
                }
                else
                {
                    query = string.Format(queryTemplateNoCondition, tableName, setClause);
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Transaction = transaction;

                    // Add parameters for SET clause
                    foreach (var prop in setProperties)
                    {
                        command.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(entity) ?? DBNull.Value);
                    }

                    // Add parameter for primary key if provided
                    if (!string.IsNullOrEmpty(primaryKey))
                    {
                        var primaryKeyValue = typeof(T).GetProperty(primaryKey)?.GetValue(entity);
                        command.Parameters.AddWithValue("@" + primaryKey, primaryKeyValue ?? DBNull.Value);
                    }

                    // Add additional parameters for WHERE condition
                    if (additionalParameters != null && additionalParameters.Count > 0)
                    {
                        int filterIndex = 0;
                        foreach (var param in additionalParameters)
                        {
                            string filterName = $"@param{filterIndex}";
                            command.Parameters.AddWithValue(filterName, param ?? DBNull.Value);
                            filterIndex++;
                        }
                    }

                    return await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating data: " + ex.Message, ex);
            }
        }



        public async Task<int> DeleteAsync(
            List<object> filter,
            string tableName,
            SqlConnection connection,
            SqlTransaction transaction,
            string whereConditions = null)
        {
            // Validate table name to prevent SQL injection
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
            {
                throw new ArgumentException("Invalid table name.", nameof(tableName));
            }

            try
            {
                // Ensure WHERE condition is provided if filters are present
                if (filter.Count > 0 && string.IsNullOrWhiteSpace(whereConditions))
                {
                    throw new ArgumentException("WHERE conditions in Deleting Data are required when filters are provided.");
                }

                const string queryTemplateWithoutWhere = "DELETE FROM [{0}]";
                const string queryTemplateWithWhere = "DELETE FROM [{0}] WHERE {1}";

                string query = string.IsNullOrWhiteSpace(whereConditions)
                    ? string.Format(queryTemplateWithoutWhere, tableName)
                    : string.Format(queryTemplateWithWhere, tableName, whereConditions);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Transaction = transaction;

                    // Add parameters for filters if any
                    for (int i = 0; i < filter.Count; i++)
                    {
                        string paramName = $"@param{i}";
                        command.Parameters.AddWithValue(paramName, filter[i] ?? DBNull.Value);
                    }

                    // Execute query and return the number of affected rows
                    return await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while deleting data: " + ex.Message, ex);
            }
        }



        public async Task CommitAndRollbackMethod(Func<SqlConnection, SqlTransaction, Task> action)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //Execute the action with the transaction and connection
                        await action(connection, transaction);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback if there's an error
                        MessageBox.Show(
                            $"RollBack Complete An Error encounter:\n\n{ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }
    }
}
