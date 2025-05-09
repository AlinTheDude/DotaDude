// 1. Aggiorna RegisterModel.cs per aggiungere il debug e il riferimento al DbContext

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DotaDude.Data;  // Aggiungi questo
using DotaDude.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotaDude.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ILogger<RegisterModel> _logger;
        private readonly DotaDudeDbContext _dbContext; // Aggiungi questo

        public RegisterModel(AuthService authService, ILogger<RegisterModel> logger, DotaDudeDbContext dbContext)
        {
            _authService = authService;
            _logger = logger;
            _dbContext = dbContext; // Inizializza DbContext
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Il nome utente è obbligatorio")]
            [StringLength(50, ErrorMessage = "Il {0} deve essere di almeno {2} e massimo {1} caratteri.", MinimumLength = 3)]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "L'email non è valida")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La password è obbligatoria")]
            [StringLength(100, ErrorMessage = "La {0} deve essere di almeno {2} e massimo {1} caratteri.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Conferma password")]
            [Compare("Password", ErrorMessage = "La password e la conferma password non corrispondono.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Ruolo preferito")]
            public string PreferredRole { get; set; } = "carry";
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            // DEBUG: Verifica connessione al database
            try
            {
                var dbExists = _dbContext.Database.CanConnect();
                ViewData["DbStatus"] = dbExists ? "Connessione al database riuscita" : "Impossibile connettersi al database";

                // Tenta di recuperare tutti gli utenti
                var usersCount = _dbContext.Users.Count();
                ViewData["UsersCount"] = $"Utenti nel database: {usersCount}";

                // Verifica il percorso del database
                var connectionString = _dbContext.Database.GetConnectionString();
                ViewData["DbPath"] = connectionString ?? "Connection string non disponibile";
            }
            catch (Exception ex)
            {
                ViewData["DbError"] = $"Errore database: {ex.Message}";
                _logger.LogError(ex, "Errore durante la verifica del database");
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ModelState.Remove("returnUrl"); // Come hai già fatto

            returnUrl ??= Url.Content("~/");

            try
            {
                if (ModelState.IsValid)
                {
                    _logger.LogInformation($"Tentativo di registrazione per: {Input.Username}, {Input.Email}, Ruolo: {Input.PreferredRole}");

                    var result = await _authService.RegisterUserAsync(Input.Username, Input.Email, Input.Password, Input.PreferredRole);

                    if (result)
                    {
                        _logger.LogInformation($"Registrazione completata per: {Input.Username}");

                        // Rimuovi il login automatico e reindirizza alla pagina di login
                        // con un messaggio di successo
                        TempData["SuccessMessage"] = "Account creato con successo! Ora puoi accedere con le tue credenziali.";
                        return RedirectToPage("./Login");
                    }
                    else
                    {
                        _logger.LogWarning($"Registrazione fallita per: {Input.Username}, {Input.Email}");
                        ModelState.AddModelError(string.Empty, "Nome utente o email già in uso.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante la registrazione per: {Input?.Username}: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore durante la registrazione. Riprova più tardi.");
            }

            // Se siamo arrivati fin qui, c'è stato un errore, mostriamo di nuovo il form
            return Page();
        }
    }
}