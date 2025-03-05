using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem
{
    public static class DatabaseConfig
    {
        public static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
    }

}
