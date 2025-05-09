using System;
using System.Threading.Tasks;
using DotaDude.Models;
using DotaDude.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DotaDude.Pages
{
    [Authorize] // Questo attributo richiede l'autenticazione
    public class DashboardModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(AuthService authService, ILogger<DashboardModel> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public User CurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                CurrentUser = await _authService.GetCurrentUserAsync();

                if (CurrentUser == null)
                {
                    _logger.LogWarning("Accesso alla dashboard negato: utente non trovato");
                    return RedirectToPage("/Login");
                }

                _logger.LogInformation($"Dashboard caricata per l'utente: {CurrentUser.Username}");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il caricamento della dashboard");
                return RedirectToPage("/Error");
            }
        }

        public string GetRoleName(string role)
        {
            return role?.ToLower() switch
            {
                "carry" => "Carry",
                "mid" => "Mid",
                "offlane" => "Offlane",
                "support" => "Support",
                _ => "Unknown"
            };
        }
    }
}