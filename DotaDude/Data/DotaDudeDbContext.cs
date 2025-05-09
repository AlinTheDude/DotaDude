using DotaDude.Models;
using Microsoft.EntityFrameworkCore;

namespace DotaDude.Data
{
    public class DotaDudeDbContext : DbContext
    {
        public DotaDudeDbContext(DbContextOptions<DotaDudeDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomHero> CustomHeroes { get; set; }
        public DbSet<CustomHeroAbility> CustomHeroAbilities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurazione dell'entità User
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            // Configura la proprietà PreferredRole
            modelBuilder.Entity<User>()
                .Property(u => u.PreferredRole)
                .HasDefaultValue("carry");
            // Configura LastLoginDate come nullable
            modelBuilder.Entity<User>()
                .Property(u => u.LastLoginDate)
                .IsRequired(false);
            // Configura Email come nullable per l'utente admin (che potrebbe non avere email)
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired(false);

            // Configurazione entità CustomHero
            modelBuilder.Entity<CustomHero>()
                .HasKey(h => h.Id);

            // Configurazione relazione CustomHero e CustomHeroAbility
            modelBuilder.Entity<CustomHeroAbility>()
                .HasKey(a => a.Id);
            modelBuilder.Entity<CustomHeroAbility>()
                .HasOne(a => a.CustomHero)
                .WithMany(h => h.Abilities)
                .HasForeignKey(a => a.CustomHeroId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seeding dei dati iniziali, incluso l'utente admin
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@dotadude.com", // Fornisci un'email per l'admin
                    // Password = "admin123" (hashata con SHA256)
                    PasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9",
                    RegistrationDate = DateTime.Now,
                    AvatarUrl = "/images/avatars/admin.png",
                    IsActive = true,
                    PreferredRole = "support" // Un valore di default
                },
                new User
                {
                    Id = 2,
                    Username = "demouser",
                    Email = "demo@example.com",
                    // Password = "password123" (hashata)
                    PasswordHash = "ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f",
                    RegistrationDate = DateTime.Now.AddDays(-30),
                    AvatarUrl = "/images/avatars/default.png",
                    IsActive = true,
                    PreferredRole = "carry"
                }
            );
        }
    }
}