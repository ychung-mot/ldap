using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ldapmain
{
    public class AdAccount
    {
        public string Username { get; set; }
        public Guid UserGuid { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Company { get; set; }
        public string Department { get; set; }
        public List<string> MemberOf { get; set; }
    }
}
