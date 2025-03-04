using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.common
{
    public class GetComboBoxAttributes
    {
        private readonly string connectionString;

        public GetComboBoxAttributes(string connectionString)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task GetComboBoxAttributeValueAsync(ComboBox comboBox, string query)
        {
            comboBox.Items.Clear(); // Clear existing items

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(); // Buksan ang koneksyon

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // I-execute ang query at kunin ang data
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
