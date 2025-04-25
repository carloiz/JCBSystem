using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.common;


namespace JCBSystem.Users
{
    public partial class UsersListForm : Form
    {

        private readonly DataManager dataManager = new DataManager();

        private readonly Modules modules = new Modules();

        private readonly Pagination pagination = new Pagination();

        private string userNumber;

        public UsersListForm()
        {
            InitializeComponent();
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
                if (column.Name == "UserNumber" || column.Name == "Username" || column.Name == "UserLevel" || column.Name == "Status" || column.Name == "IsSessionActive")
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
            userNumber = string.Empty;

            if (e.Button == MouseButtons.Right)
            {
                cms1.Show(Cursor.Position);
            }

            if (e.Button == MouseButtons.Right) // Check kung right-click
            {
                var hit = dataGridView1.HitTest(e.X, e.Y); // Alamin kung anong row ang na-click
                if (hit.RowIndex >= 0) // Siguraduhin na valid ang row index
                {
                    dataGridView1.ClearSelection(); // I-clear ang ibang selections
                    dataGridView1.Rows[hit.RowIndex].Selected = true; // I-select ang row

                    // Kunin ang value ng "ID" column
                    object idValue = dataGridView1.Rows[hit.RowIndex].Cells["UserNumber"].Value;

                    userNumber = idValue.ToString();

                }
            }

            if (string.IsNullOrEmpty(userNumber))
            {
                updateToolStripMenuItem.Visible = false;
                return;
            }

            updateToolStripMenuItem.Visible = true;
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserManagementForm user = new UserManagementForm(this , true);
            FormHelper.OpenFormWithFade(user, true);

        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserManagementForm user = new UserManagementForm(this, false, userNumber);
            FormHelper.OpenFormWithFade(user, true);
        }
    }
}
