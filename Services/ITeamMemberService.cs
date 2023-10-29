using ProactiveBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProactiveBot.Services
{
    public interface ITeamMemberService
    {

        Task<bool> AddTeamMemberListAsync(IList<TeamMember> memberList); 

        Task<TeamMember> AddTeamMemberAsync(TeamMember member); // POST New TeamMember
        
        Task<TeamMember> GetTeamMemberAsync(string id); // GET Single TeamMember
    }
}
