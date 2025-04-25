using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.common;

namespace JCBSystem.Users
{
    public partial class UserManagementForm: Form
    {
        private readonly DataManager dataManager = new DataManager();
        private readonly CheckIfRecordExists recordExists = new CheckIfRecordExists();
        private readonly GenerateNextValues values = new GenerateNextValues();
        private readonly UsersListForm listForm;
        private readonly bool isNewRecord;
        private readonly string userNumber;
        private readonly SystemDate date = new SystemDate();



        public UserManagementForm(UsersListForm listForm, bool isNewRecord, string userNumber = null)
        {
            InitializeComponent();
            this.listForm = listForm;
            this.isNewRecord = isNewRecord;
            this.userNumber = userNumber;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) 
                || string.IsNullOrEmpty(txtPassword.Text) 
                || string.IsNullOrEmpty(txtRepassword.Text) 
                || string.IsNullOrEmpty(cbRole.Text))
            {
                MessageBox.Show(
                    "Fill-Up All Fields.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (txtPassword.Text != txtRepassword.Text)
            {
                MessageBox.Show(
                    "Password and Retype Password Must Same.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            bool isExist = await recordExists.CheckIfRecordExistsAsync(
                new List<object> { txtUsername.Text },
                "Users",
                "Username = @param0"
            );

            if (isExist)
            {
                MessageBox.Show(
                    "Username Already Exist.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessCreate(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }


        private async Task ProcessCreate(IDbConnection connection, IDbTransaction transaction)
        {
            string password = PasswordHelper.HashPassword(txtPassword.Text);

            string userId = await values.GenerateNextIdAsync("Users", "UserNumber", "U");

            DateTime dateToday = date.GetPhilippineTime();

            var userCreateDto = new UserCreateDto
            {
                UserNumber = userId,
                Username = txtUsername.Text,
                Password = password,
                UserLevel = cbRole.SelectedItem.ToString(),
                Status = true,
                IsSessionActive = false,
                CurrentToken = null,
                RecordDate = dateToday
            };

            await dataManager.InsertAsync(userCreateDto, "Users", connection, transaction);


            transaction.Commit(); // Commit changes  

            listForm.get_all_data();

            // Display the message for successful shift start
            MessageBox.Show("Successfully Add New Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FormHelper.CloseFormWithFade(this);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FormHelper.CloseFormWithFade(this);
        }

        private void UserManagementForm_Load(object sender, EventArgs e)
        {
            if (isNewRecord)
            {
                this.button1.Enabled = true;
                this.button2.Enabled = false;
            }
            else
            {
                this.button1.Enabled = false;
                this.button2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}
