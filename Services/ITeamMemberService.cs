using ProactiveBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProactiveBot.Services
{
    public interface ITeamMemberService
    {

        Task AddTeamMemberListAsync(IList<TeamMemberInfo> memberList);

        Task<TeamMemberInfo> FindTeamMemberAsync(string prefix);

        Task<TeamMemberInfo> GetTeamMemberAsync(string upn); // GET Single TeamMember
    }
}
