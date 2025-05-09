using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DotaDude.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using DotaDude.Models;
using System.Security.Cryptography;
using System.Text;

namespace DotaDude.Pages
{
    public class AdminLoginModel : PageModel
    {
        private readonly ILogger<AdminLoginModel> _logger;
        private readonly DotaDudeDbContext _context;

        public AdminLoginModel(ILogger<AdminLoginModel> logger, DotaDudeDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Il nome utente è obbligatorio")]
            [Display(Name = "Nome Admin")]
            public string AdminUsername { get; set; }

            [Required(ErrorMessage = "La password è obbligatoria")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verifica se ci sono già utenti nel database
            var userCount = await _context.Users.CountAsync();
            _logger.LogInformation($"Utenti nel database: {userCount}");

            // Verifica se l'utente admin esiste
            var adminExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == "admin");
            _logger.LogInformation($"Utente admin esistente: {adminExists}");

            // Se non esiste alcun utente admin, registra un avviso
            if (!adminExists)
            {
                _logger.LogWarning("Utente admin non trovato nel database. Verifica la configurazione del database.");
                // Opzionale: Scommentare la seguente linea se vuoi mostrare un messaggio all'utente
                // ModelState.AddModelError(string.Empty, "Configurazione del sistema incompleta. Contattare l'amministratore.");
            }

            // Se c'è già un messaggio di errore fornito dalla pagina precedente
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _logger.LogInformation($"Tentativo di login amministrativo per l'utente: {Input.AdminUsername}");

                // Debug: Stampa tutti gli utenti nel database
                var allUsers = await _context.Users.ToListAsync();
                _logger.LogInformation($"Utenti nel database: {allUsers.Count}");
                foreach (var user in allUsers)
                {
                    _logger.LogInformation($"ID: {user.Id}, Utente: {user.Username}, IsActive: {user.IsActive}, PasswordHash: {user.PasswordHash}");
                }

                // Calcola l'hash della password inserita
                string hashedPassword = HashPassword(Input.Password);
                _logger.LogInformation($"Password inserita dall'utente: {Input.Password}");
                _logger.LogInformation($"Hash password inserita: {hashedPassword}");

                // SOLUZIONE TEMPORANEA: Bypass diretto per l'utente admin
                if (Input.AdminUsername.ToLower() == "admin" && Input.Password == "admin123")
                {
                    _logger.LogWarning("Utilizzando bypass di autenticazione temporaneo per l'utente admin");

                    // Imposta il cookie admin
                    Response.Cookies.Append("IsAdmin", "true", new Microsoft.AspNetCore.Http.CookieOptions
                    {
                        Expires = DateTime.Now.AddHours(2),
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax
                    });

                    _logger.LogInformation("Accesso amministrativo riuscito tramite bypass");
                    return RedirectToPage("/AdminDashboard");
                }

                // Verifica le credenziali admin normalmente
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == Input.AdminUsername.ToLower());

                if (adminUser == null)
                {
                    _logger.LogWarning($"Utente admin non trovato: {Input.AdminUsername}");
                    ModelState.AddModelError(string.Empty, "Nome utente o password non validi.");
                    return Page();
                }

                if (!adminUser.IsActive)
                {
                    _logger.LogWarning($"Utente admin non attivo: {Input.AdminUsername}");
                    ModelState.AddModelError(string.Empty, "Account disattivato. Contattare l'amministratore.");
                    return Page();
                }

                _logger.LogInformation($"Password stored hash: {adminUser.PasswordHash}");

                if (hashedPassword != adminUser.PasswordHash)
                {
                    _logger.LogWarning($"Password non valida per l'utente admin: {Input.AdminUsername}");
                    ModelState.AddModelError(string.Empty, "Nome utente o password non validi.");
                    return Page();
                }

                // Imposta il cookie admin
                Response.Cookies.Append("IsAdmin", "true", new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Expires = DateTime.Now.AddHours(2),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax
                });

                _logger.LogInformation($"Accesso amministrativo riuscito per: {Input.AdminUsername}");
                return RedirectToPage("/AdminDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante il tentativo di login amministrativo per: {Input.AdminUsername}");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore durante il login. Riprova più tardi.");
                return Page();
            }
        }

        // Metodo di hash della password - DEVE essere identico a quello in Program.cs
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}