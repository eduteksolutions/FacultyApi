using FacultyApi.model;
using Microsoft.EntityFrameworkCore;

namespace FacultyApi.Data

{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>  options):base(options)
        { 
            
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }
        public DbSet<HRDCardAttendance> HRDCardAttendances { get; set; }

    }
}
