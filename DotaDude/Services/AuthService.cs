using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotaDude.Data;
using DotaDude.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotaDude.Services
{
    public class AuthService
    {
        private readonly DotaDudeDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(DotaDudeDbContext dbContext, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<bool> RegisterUserAsync(string username, string email, string password, string preferredRole = "carry")
        {
            try
            {
                // Verifica se l'utente esiste già
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email.ToLower());

                if (existingUser != null)
                {
                    _logger.LogWarning($"Tentativo di registrazione con username/email già esistente: {username}, {email}");
                    return false;
                }

                // Crea nuovo utente
                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = HashPassword(password),
                    RegistrationDate = DateTime.Now,
                    AvatarUrl = "/images/avatars/default.png", // Avatar predefinito
                    IsActive = true,
                    PreferredRole = preferredRole // Salviamo il ruolo preferito
                };

                _logger.LogInformation($"Tentativo di salvare l'utente: {newUser.Username}");

                await _dbContext.Users.AddAsync(newUser);
                int rowsAffected = await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Risultato salvataggio: {rowsAffected} record salvati");

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Utente registrato con successo: {username}, ID: {newUser.Id}, Ruolo: {preferredRole}");
                    return true;
                }
                else
                {
                    _logger.LogWarning("SaveChangesAsync non ha salvato alcun record");
                    return false;
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Errore DB durante la registrazione dell'utente: {username}, Messaggio: {dbEx.InnerException?.Message ?? dbEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore generico durante la registrazione dell'utente: {username}, Messaggio: {ex.Message}");
                return false;
            }
        }

        public async Task<User> ValidateUserAsync(string usernameOrEmail, string password)
        {
            _logger.LogInformation($"Inizio validazione utente: {usernameOrEmail}");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
                (u.Username.ToLower() == usernameOrEmail.ToLower() ||
                 u.Email.ToLower() == usernameOrEmail.ToLower()) &&
                u.IsActive);

            if (user == null)
            {
                _logger.LogWarning($"Utente non trovato o inattivo: {usernameOrEmail}");
                return null;
            }

            if (VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogInformation($"Password valida per l'utente: {usernameOrEmail}");
                return user;
            }

            _logger.LogWarning($"Password errata per l'utente: {usernameOrEmail}");
            return null;
        }

        public async Task SignInAsync(User user, bool rememberMe = false)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("PreferredRole", user.PreferredRole ?? "carry") // Aggiungi il ruolo preferito ai claims
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe, // Usa il valore rememberMe per decidere se il cookie è persistente
                    ExpiresUtc = rememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30) // Se rememberMe è true, conserva per 30 giorni
                        : DateTimeOffset.UtcNow.AddHours(2), // Altrimenti conserva per 2 ore (sessione breve)
                    AllowRefresh = true,
                    // Aggiungi un ReturnUrl esplicito per la dashboard
                    RedirectUri = "/Dashboard"
                };

                _logger.LogInformation($"Prima di SignInAsync - User: {user.Username}, RememberMe: {rememberMe}");

                await _httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"Utente autenticato con successo: {user.Username} (RememberMe: {rememberMe})");

                // Aggiorna LastLoginDate
                user.LastLoginDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante SignInAsync per l'utente: {user.Username}");
                throw;
            }
        }

        public async Task SignOutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Utente disconnesso");
        }

        public async Task<User> GetCurrentUserAsync()
        {
            try
            {
                if (_httpContextAccessor.HttpContext?.User?.Identity == null ||
                    !_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
                {
                    return null;
                }

                var userIdClaim = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var user = await _dbContext.Users.FindAsync(userId);
                    if (user != null)
                    {
                        _logger.LogInformation($"Utente corrente recuperato: {user.Username}");
                        return user;
                    }
                    else
                    {
                        _logger.LogWarning($"Utente con ID {userId} non trovato nel database");
                    }
                }
                else
                {
                    _logger.LogWarning("Claim NameIdentifier non trovato o non valido");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dell'utente corrente");
                return null;
            }
        }

        public User GetCurrentUser()
        {
            return GetCurrentUserAsync().Result;
        }

        // Metodi per gestire le password in modo sicuro
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }  
    }
}