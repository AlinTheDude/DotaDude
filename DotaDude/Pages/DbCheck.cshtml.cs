// Metodo di utilità da aggiungere alla classe AdminLoginModel o in una pagina temporanea per debug
// Crea una pagina Razor temporanea chiamata DbCheck.cshtml e DbCheck.cshtml.cs con questo codice

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DotaDude.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotaDude.Models;
using System;

namespace DotaDude.Pages
{
    public class DbCheckModel : PageModel
    {
        private readonly DotaDudeDbContext _context;

        public DbCheckModel(DotaDudeDbContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; } = new List<User>();
        public string Message { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verifica se il database esiste
            bool dbExists = await _context.Database.CanConnectAsync();
            Message = dbExists ? "Connessione al database riuscita." : "Impossibile connettersi al database!";

            // Recupera tutti gli utenti
            Users = await _context.Users.ToListAsync();

            // Se non ci sono utenti admin, creane uno
            if (!Users.Exists(u => u.Username.ToLower() == "admin"))
            {
                try
                {
                    var hashedPassword = HashPassword("admin123");
                    _context.Users.Add(new User
                    {
                        Username = "admin",
                        PasswordHash = hashedPassword,
                        IsActive = true,
                        RegistrationDate = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    Message += " Utente admin creato con successo!";

                    // Aggiorna la lista degli utenti
                    Users = await _context.Users.ToListAsync();
                }
                catch (Exception ex)
                {
                    Message += $" Errore durante la creazione dell'utente admin: {ex.Message}";
                }
            }

            return Page();
        }

        // Crea un nuovo utente di test
        public async Task<IActionResult> OnPostCreateTestUserAsync()
        {
            try
            {
                var testUser = new User
                {
                    Username = $"test_{DateTime.Now.Ticks}",
                    PasswordHash = HashPassword("test123"),
                    IsActive = true,
                    RegistrationDate = DateTime.Now,
                    PreferredRole = "Tester"
                };

                _context.Users.Add(testUser);
                await _context.SaveChangesAsync();
                Message = $"Utente di test {testUser.Username} creato con successo!";
            }
            catch (Exception ex)
            {
                Message = $"Errore durante la creazione dell'utente di test: {ex.Message}";
            }

            Users = await _context.Users.ToListAsync();
            return Page();
        }

        // Metodo per resettare il database
        public async Task<IActionResult> OnPostResetDatabaseAsync()
        {
            try
            {
                // Elimina tutti gli utenti tranne admin
                var nonAdminUsers = await _context.Users
                    .Where(u => u.Username.ToLower() != "admin")
                    .ToListAsync();

                _context.Users.RemoveRange(nonAdminUsers);
                await _context.SaveChangesAsync();

                // Assicurati che esista l'utente admin
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "admin");
                if (adminUser == null)
                {
                    var hashedPassword = HashPassword("admin123");
                    _context.Users.Add(new User
                    {
                        Username = "admin",
                        PasswordHash = hashedPassword,
                        IsActive = true,
                        RegistrationDate = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                Message = "Database resettato con successo!";
            }
            catch (Exception ex)
            {
                Message = $"Errore durante il reset del database: {ex.Message}";
            }

            Users = await _context.Users.ToListAsync();
            return Page();
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}