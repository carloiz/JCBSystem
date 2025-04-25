using JCBSystem.Connection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.common
{
    public class GetComboBoxAttributes
    {
        private readonly IDbConnectionFactory _connectionFactory;

        private readonly ConnectionAsync async = new ConnectionAsync();

        public GetComboBoxAttributes()
        {
            this._connectionFactory = ConnectionFactorySelector.GetFactory();
        }

        public async Task GetComboBoxAttributeValueAsync(ComboBox comboBox, string query)
        {
            comboBox.Items.Clear(); // Clear existing items

            using (var connection = _connectionFactory.CreateConnection())
            {
                await async.OpenConnectionAsync(connection);

                var isOdbc = connection is OdbcConnection;

                string finalQuery = Modules.ReplaceSharpWithParams(query, isOdbc);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = finalQuery;
                    // I-execute ang query at kunin ang dataS
                    if (command is DbCommand dbCommand)
                    {
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            // Basahin ang mga resulta at idagdag sa comboBox
                            while (await reader.ReadAsync())
                            {
                                // Halimbawa, i-add ang value mula sa unang column
                                comboBox.Items.Add(reader[0].ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}
