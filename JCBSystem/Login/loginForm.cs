using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.common;
using JCBSystem.Login;
using JCBSystem.Users;


namespace JCBSystem
{
    public partial class loginForm : Form
    {
        private readonly RegistryKeys registryKeys = new RegistryKeys();

        private readonly GetFieldsValues getFieldsValues = new GetFieldsValues();

        private readonly DataManager dataManager = new DataManager();
        private readonly MainForm mainForm;
        private readonly string subKey;


        public loginForm(MainForm mainForm,string subKey)
        {
            InitializeComponent();
            this.mainForm = mainForm;
            this.subKey = subKey;
        }


        private async Task<(bool, string, string)> IsUserLoggedIn()
        {

            var userRegistInfo = await registryKeys.GetRegistLocalSession<UserRegistInfo>(subKey);

            if (userRegistInfo != null &&
                !string.IsNullOrEmpty(userRegistInfo.AuthToken) &&
                !string.IsNullOrEmpty(userRegistInfo.UserNumber) &&
                !string.IsNullOrEmpty(userRegistInfo.UserLevel))
            {
                try
                {
                    string token = DataProtectorHelper.Unprotect(userRegistInfo.AuthToken);
                    string usernumber = DataProtectorHelper.Unprotect(userRegistInfo.UserNumber);
                    string userlevel = DataProtectorHelper.Unprotect(userRegistInfo.UserLevel);

                    return (true, token, usernumber);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Decryption failed: {ex.Message}");
                    Application.Exit();
                }
            }

            Console.WriteLine("User is not authenticated.");
            return (false, null, null);
        }


        public async Task ServiceLogin()
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await Process(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task Process(SqlConnection connection, SqlTransaction transaction)
        {
            var (userLoggedIn, existingToken, usernumber) = await IsUserLoggedIn();

            // Check if the user is already authenticated
            if (userLoggedIn)
            {
                if (!JwtTokenHelper.IsTokenExpired(existingToken))
                {
                    throw new ArgumentNullException("Login Failed, User is already authenticated: Local Session");
                }

                Console.WriteLine("Token Already Expired.");
            }

            Dictionary<string, object> GetValues = await
                getFieldsValues.GetFieldValuesAsync(
                    new List<object> { txtUsername.Text }, // Parameters
                    "Users",
                    new List<string> { "Password", "IsSessionActive", "Status", "UserLevel", "UserNumber" }, // this is for like SUM(Quantity) As TotalQuantity
                    new List<string> { "Password", "IsSessionActive", "Status", "UserLevel", "UserNumber" }, // this is fix where the name of field
                    "Username = @param0"
                );

            // Call The Value
            string userPassword =
                GetValues.ContainsKey("Password") &&
                !string.IsNullOrEmpty(GetValues["Password"]?.ToString())
                ? Convert.ToString(GetValues["Password"])
                : string.Empty;

            string userNumber =
                GetValues.ContainsKey("UserNumber") &&
                !string.IsNullOrEmpty(GetValues["UserNumber"]?.ToString())
                ? Convert.ToString(GetValues["UserNumber"])
                : string.Empty;

            string userLevel =
                GetValues.ContainsKey("UserLevel") &&
                !string.IsNullOrEmpty(GetValues["UserLevel"]?.ToString())
                ? Convert.ToString(GetValues["UserLevel"])
                : string.Empty;

            bool userSession =
                GetValues.ContainsKey("IsSessionActive") &&
                !string.IsNullOrEmpty(GetValues["IsSessionActive"]?.ToString())
                ? Convert.ToBoolean(GetValues["IsSessionActive"])
                : false;

            bool userStatus =
                GetValues.ContainsKey("Status") &&
                !string.IsNullOrEmpty(GetValues["Status"]?.ToString())
                ? Convert.ToBoolean(GetValues["Status"])
                : false;


            if (userPassword == null || !PasswordHelper.VerifyPassword(txtPassword.Text, userPassword))
            {
                throw new ArgumentNullException("Login Failed, Incorrect Username or Password");
            }


            // Check if the user's database session is already active
            if (userSession && userLoggedIn == false)
            {
                throw new ArgumentNullException("Login Failed, User is already authenticated: Database Session");
            }

            // Check User Status
            if (!userStatus)
            {
                throw new ArgumentNullException("Login failed: User has been deactivated.");
            }

            Dictionary<string, string> keyValues = new Dictionary<string, string>
            {
                { "Username", txtUsername.Text },
                { "UserLevel", txtPassword.Text }
            };

            var tokenString = JwtTokenHelper.GetJWTToken(keyValues);

            /////// FOR PRIMARY KEY ONLY 1 DATA UPDATE
            var userDto = new UserUpdateDto
            {
                UserNumber = userNumber, // always have this for Primary Key
                IsSessionActive = true,
                CurrentToken = PasswordHelper.HashPassword(tokenString)
            };

            await dataManager.UpdateAsync(
                entity: userDto,
                tableName: "Users",
                connection: connection,
                transaction: transaction,
                primaryKey: "UserNumber"
            );


            // Write to the registry
            var userRegistInfo = new UserRegistInfo
            {
                AuthToken = DataProtectorHelper.Protect(tokenString),
                UserNumber = DataProtectorHelper.Protect(userNumber),
                UserLevel = DataProtectorHelper.Protect(userLevel),
            };

            await registryKeys.CreateRegistLocalSession(userRegistInfo, subKey);

            transaction.Commit(); // Commit changes

            mainForm.userIsLogin();

            FormHelper.CloseFormWithFade(this);
        }


        private async void loginBtn_Click(object sender, EventArgs e)
        {
            await ServiceLogin();
        }
    }
}
