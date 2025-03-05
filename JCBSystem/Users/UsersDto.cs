using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Users
{
    public class UsersDto
    {
        public string UserNumber { get; set; }
        public string Username { get; set; }
        public string UserLevel { get; set; }
        public string Status { get; set; }
        public bool IsSessionActive { get; set; }
        public DateTime RecordDate { get; set; }
    }


    public class UserUpdateDto
    {
        public string UserNumber { get; set; }
        public bool IsSessionActive { get; set; }
        public string CurrentToken { get; set; }
    }

    public class UserCreateDto
    {
        public string UserNumber { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string UserLevel { get; set; }
        public bool Status { get; set; }
        public bool IsSessionActive { get; set; }
        public string CurrentToken { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
