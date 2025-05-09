using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DotaDude.Data;
using DotaDude.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace DotaDude.Pages
{
    public class AdminDashboardModel : PageModel
    {
        private readonly DotaDudeDbContext _context;
        private readonly ILogger<AdminDashboardModel> _logger;

        public AdminDashboardModel(DotaDudeDbContext context, ILogger<AdminDashboardModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<User> Users { get; set; } = new List<User>();
        public List<Hero> Heroes { get; set; } = new List<Hero>();
        public SiteStatistics Statistics { get; set; } = new SiteStatistics();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Verifica se l'utente è autenticato come admin
                if (!Request.Cookies.ContainsKey("IsAdmin"))
                {
                    _logger.LogWarning("Tentativo di accesso al dashboard admin senza autorizzazione");
                    return RedirectToPage("/AdminLogin");
                }

                // Carica gli utenti dal database
                Users = await _context.Users
                    .OrderByDescending(u => u.RegistrationDate)
                    .ToListAsync();

                // Per gli eroi, crea una lista temporanea con i dati di Dota2
                // In una implementazione reale, dovresti avere una tabella Heroes nel database
                Heroes = GetHeroes();

                // Calcola le statistiche del sito
                Statistics = CalculateSiteStatistics(Users);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il caricamento del dashboard admin");
                StatusMessage = "Si è verificato un errore durante il caricamento dei dati.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleUserStatusAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    StatusMessage = "Utente non trovato.";
                    return RedirectToPage();
                }

                // Inverte lo stato attivo dell'utente
                user.IsActive = !user.IsActive;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                StatusMessage = $"Stato dell'utente {user.Username} aggiornato con successo.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante l'aggiornamento dello stato dell'utente ID: {userId}");
                StatusMessage = "Si è verificato un errore durante l'aggiornamento dello stato dell'utente.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    StatusMessage = "Utente non trovato.";
                    return RedirectToPage();
                }

                // Verifica che non sia l'ultimo admin
                if (user.Username.ToLower() == "admin" && await _context.Users.CountAsync(u => u.Username.ToLower().Contains("admin")) <= 1)
                {
                    StatusMessage = "Impossibile eliminare l'ultimo amministratore.";
                    return RedirectToPage();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Utente {user.Username} (ID: {userId}) eliminato con successo");
                StatusMessage = $"Utente {user.Username} eliminato con successo.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante l'eliminazione dell'utente ID: {userId}");
                StatusMessage = "Si è verificato un errore durante l'eliminazione dell'utente.";
                return RedirectToPage();
            }
        }

        // In una implementazione reale, questo verrebbe dal database
        private List<Hero> GetHeroes()
        {
            return new List<Hero>
            {
                new Hero { Id = 1, Name = "Anti-Mage", Attribute = "Agility", Role = "Carry", ImageUrl = "/images/heroes/anti-mage.png", Popularity = 56.78 },
                new Hero { Id = 2, Name = "Axe", Attribute = "Strength", Role = "Initiator", ImageUrl = "/images/heroes/axe.png", Popularity = 62.14 },
                new Hero { Id = 3, Name = "Crystal Maiden", Attribute = "Intelligence", Role = "Support", ImageUrl = "/images/heroes/crystal-maiden.png", Popularity = 48.92 },
                new Hero { Id = 4, Name = "Invoker", Attribute = "Intelligence", Role = "Nuker", ImageUrl = "/images/heroes/invoker.png", Popularity = 78.35 },
                new Hero { Id = 5, Name = "Juggernaut", Attribute = "Agility", Role = "Carry", ImageUrl = "/images/heroes/juggernaut.png", Popularity = 67.21 },
                new Hero { Id = 6, Name = "Pudge", Attribute = "Strength", Role = "Disabler", ImageUrl = "/images/heroes/pudge.png", Popularity = 83.49 },
                new Hero { Id = 7, Name = "Shadow Fiend", Attribute = "Agility", Role = "Nuker", ImageUrl = "/images/heroes/shadow-fiend.png", Popularity = 59.67 },
                new Hero { Id = 8, Name = "Techies", Attribute = "Universal", Role = "Nuker", ImageUrl = "/images/heroes/techies.png", Popularity = 25.18 },
                new Hero { Id = 9, Name = "Phantom Assassin", Attribute = "Agility", Role = "Carry", ImageUrl = "/images/heroes/phantom-assassin.png", Popularity = 72.43 },
                new Hero { Id = 10, Name = "Zeus", Attribute = "Intelligence", Role = "Nuker", ImageUrl = "/images/heroes/zeus.png", Popularity = 51.79 }
            };
        }

        private SiteStatistics CalculateSiteStatistics(List<User> users)
        {
            var stats = new SiteStatistics
            {
                TotalUsers = users.Count,
                TotalActiveUsers = users.Count(u => u.IsActive),
                TotalHeroes = Heroes.Count,
                // In una implementazione reale, otterresti questi dati dal database
                TotalMatchesPlayed = 12783,
                // Utilizziamo una formula per rendere la data più realistica
                LastDatabaseUpdate = DateTime.Now.AddHours(-new Random().Next(1, 24)),
                ActiveUsersLastWeek = users.Count(u => u.LastLoginDate.HasValue && u.LastLoginDate.Value > DateTime.Now.AddDays(-7)),
                NewUsersLastMonth = users.Count(u => u.RegistrationDate > DateTime.Now.AddMonths(-1)),
                MostPopularRole = GetMostPopularRole(users)
            };

            return stats;
        }

        private string GetMostPopularRole(List<User> users)
        {
            if (users == null || !users.Any())
                return "N/A";

            // Raggruppa per ruolo preferito e conta le occorrenze
            var roleCounts = users
                .Where(u => !string.IsNullOrEmpty(u.PreferredRole))
                .GroupBy(u => u.PreferredRole)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            return roleCounts?.Role ?? "N/A";
        }

        public class Hero
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Attribute { get; set; }
            public string Role { get; set; }
            public string ImageUrl { get; set; }
            public double Popularity { get; set; }
        }

        public class SiteStatistics
        {
            public int TotalHeroes { get; set; }
            public int TotalUsers { get; set; }
            public int TotalActiveUsers { get; set; }
            public int TotalMatchesPlayed { get; set; }
            public DateTime LastDatabaseUpdate { get; set; }
            public int ActiveUsersLastWeek { get; set; }
            public int NewUsersLastMonth { get; set; }
            public string MostPopularRole { get; set; }
        }
    }
}