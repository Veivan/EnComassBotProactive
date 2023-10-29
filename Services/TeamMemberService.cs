using Microsoft.EntityFrameworkCore;
using ProactiveBot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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

        public async Task<TeamMember> AddTeamMemberAsync(TeamMember member)
        {
            try
            {
                await _db.TeamMembers.AddAsync(member);
                await _db.SaveChangesAsync();
                return member; 
            }
            catch (Exception ex)
            {
                return null; // An error occured
            }
        }

        public async Task<bool> AddTeamMemberListAsync(IList<TeamMember> memberList)
        {
            try
            {
                await _db.TeamMembers.AddRangeAsync(memberList);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false; // An error occured
            }
        }

        public async Task<TeamMember> GetTeamMemberAsync(string id)
        {
            try
            {
                return await _db.TeamMembers
                    .FirstOrDefaultAsync(i => i.Id == id);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
