using Microsoft.EntityFrameworkCore;

namespace ProactiveBot.Models
{
	public class TeamMemberContext : DbContext
	{
		public TeamMemberContext(DbContextOptions<TeamMemberContext> options)
			: base(options)
		{
		//	Database.EnsureDeleted();
			Database.EnsureCreated();
		}

		public DbSet<TeamMember> TeamMembers { get; set; } 

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

			modelBuilder.Entity<TeamMember>();

		}
	}
}

