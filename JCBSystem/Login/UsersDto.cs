using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Login
{
    internal class UsersDto
    {
    }

    public class LoginDto
    {
        public string Username { get; set; }
        public string UserLevel { get; set; }
        public string Password { get; set; }
    }

    public class UserRegistInfo
    {
        public string AuthToken { get; set; }
        public string UserNumber { get; set; }
        public string UserLevel { get; set; }
    }
}
