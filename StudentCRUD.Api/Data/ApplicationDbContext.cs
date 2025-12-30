using Microsoft.EntityFrameworkCore;
using StudentCRUD.Api.Models;

namespace StudentCRUD.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Students");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(s => s.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(s => s.Gender)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.HasIndex(s => s.Email)
                      .IsUnique();
            });
        }
    }
}