using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.common;

namespace JCBSystem.Users
{
    public partial class UsersListForm : Form
    {

        private readonly DataManager dataManager;

        private readonly Modules modules = new Modules();

        private readonly Pagination pagination = new Pagination();
        private readonly string conn;

        public UsersListForm(string conn)
        {
            InitializeComponent();
            this.conn = conn;
            dataManager = new DataManager(conn);
            get_all_data();

        }


        public async void get_all_data(List<string> image = null)
        {

            string countQuery = $@"
                SELECT COUNT(*)
                FROM 
                    Users
                ";

            // Query to fetch paginated data
            string dataQuery = $@"
                SELECT *
                FROM 
                    Users
                ";


            var customHeaders = new Dictionary<string, string>
            {
                { "UserNumber", "ID" },
                { "Username", "Username" },
                { "UserLevel", "Role" },
                { "Status", "Status" },
                { "IsSessionActive", "Session" },
                { "RecordDate", "Record Date" }
            };



            var (result, totalRecords) = await
                dataManager.SearchWithPaginatedAsync<UsersDto>
                (new List<object> { }, countQuery, dataQuery, dataGridView1, image, customHeaders, modules.pageNumber, modules.pageSize);

            modules.totalPages = (int)Math.Ceiling((double)totalRecords / modules.pageSize);



            pagination.UpdatePagination(panel1, modules.totalPages, modules.pageNumber, UpdateRecords, true);



            dataGridView1.ColumnHeadersVisible = (string.IsNullOrEmpty(result)) ? true : false;


            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.Name == "ShiftId" || column.Name == "xCounter")
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells; // Adjust based on content
                }
                else if (column.Name == "ImageColumn2")
                {
                    column.Width = 40;
                }
                else
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // Keep other columns evenly distributed
                }
            }
        }


        private Task UpdateRecords(int pageNumber)
        {
            modules.pageNumber = pageNumber;
            get_all_data();
            return Task.CompletedTask;
        }

        private void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                cms1.Show(Cursor.Position);
            }
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserManagementForm user = new UserManagementForm(this, conn);
            FormHelper.OpenFormWithFade(user, true);

        }
    }
}
