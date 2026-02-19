using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.Core.Models
{
    public class LoginInfo
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string LicenseId { get; set; }
        public DateTime? Expiration { get; set; }
        public List<ServerList> ServerListData { get; set; }
        public string Password { get; set; }
        public bool LastLogin { get; set; }
    }
}
