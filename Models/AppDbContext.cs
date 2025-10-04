using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Reflection.Emit;
using UserAuthManage.Domains;

namespace UserAuthManage.Models
{
    public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            var u = b.Entity<User>();
            u.Property(x => x.Name).HasMaxLength(128).IsRequired();
            u.Property(x => x.Email).HasMaxLength(256).IsRequired();
            u.Property(x => x.PasswordHash).IsRequired();

            // NOTE: NormalizedEmail is a persisted computed column (LOWER(Email))
            // created in migration with raw SQL + a UNIQUE INDEX (THE FIRST REQUIREMENT).
            u.Property<string>("NormalizedEmail").HasColumnName("NormalizedEmail").ValueGeneratedOnAddOrUpdate()
     .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore); // do not write on update
            u.HasIndex("NormalizedEmail").IsUnique();
        }
    }
}
