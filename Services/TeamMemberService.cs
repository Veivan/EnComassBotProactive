using Microsoft.EntityFrameworkCore;
using ProactiveBot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;

namespace ProactiveBot.Services
{
    public class TeamMemberService : ITeamMemberService
    {
        private readonly TeamMemberContext _db;

        public TeamMemberService(TeamMemberContext db)
        {
            _db = db;
        }

        public async Task AddTeamMemberListAsync(IList<TeamMemberInfo> memberList)
        {
            try
            {
                foreach (var member in memberList)
                {
                    var exists = await _db.TeamMembers.FirstOrDefaultAsync(i => i.Id == member.Id);
                    if (exists == null)
                    {
                        await _db.TeamMembers.AddAsync(member);
                    }
                }
                await _db.SaveChangesAsync();
             }
            catch (Exception ex)
            {
                //TODO save to Log // An error occured
            }
        }

        public async Task<TeamMemberInfo> GetTeamMemberAsync(string upn)
        {
            try
            {
                return await _db.TeamMembers.FirstOrDefaultAsync(i => i.Name.ToLower() == upn.ToLower());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<TeamMemberInfo> FindTeamMemberAsync(string prefix)
        {
            try
            {
                var member = await _db.TeamMembers.FirstOrDefaultAsync(i => i.Id.ToLower().StartsWith(prefix.ToLower()));
                member.Id = member.Id.Substring(prefix.Length);
                return member;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
