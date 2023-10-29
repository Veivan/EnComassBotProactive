using System.ComponentModel.DataAnnotations;

namespace ProactiveBot.Models
{
    public class TeamMember
    {
        [Key]
        public string Id { get; set; } // UserPrincipalName, UPN
        public string? Name { get; set; }
    }
}
