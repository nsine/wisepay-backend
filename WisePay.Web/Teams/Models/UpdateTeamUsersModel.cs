using System.Collections.Generic;

namespace WisePay.Web.Teams.Models
{
    public class UpdateTeamUsersModel
    {
        public IEnumerable<int> UserIds  { get; set; }
    }
}
