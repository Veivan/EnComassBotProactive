using Microsoft.EntityFrameworkCore;

namespace ProactiveBot.Models
{
	public class TeamMemberContext : DbContext
	{
        public TeamMemberContext(DbContextOptions<TeamMemberContext> options)
            : base(options)
        {
            //	Database.EnsureDeleted();
            // Database.EnsureCreated();
        }

        public DbSet<TeamMemberInfo> TeamMembers { get; set; }
        
        //public DbSet<TeamMemberInfo> TeamMembers => Set<TeamMemberInfo>();

        /*		protected override void OnModelCreating(ModelBuilder modelBuilder)
                {

                    modelBuilder.Entity<TeamMemberInfo>();

                } */


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TeamMemberDB;Trusted_Connection=True;");
            //optionsBuilder.UseInMemoryDatabase("TeamMemberService");
        }
    } 
}

