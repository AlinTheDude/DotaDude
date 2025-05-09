using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AspNet.Security.OpenId.Steam;
using DotaDude.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DotaDude.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(AuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username o Email è obbligatorio")]
            [Display(Name = "Nome Eroe o Email")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Password è obbligatoria")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ricordami")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Aggiungi il messaggio di successo se presente
            if (!string.IsNullOrEmpty(SuccessMessage))
            {
                ViewData["SuccessMessage"] = SuccessMessage;
            }

            ReturnUrl = returnUrl ?? Url.Content("~/Dashboard"); // Corretto il percorso
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // Imposta un valore predefinito per returnUrl se non fornito
            returnUrl ??= Url.Content("~/Dashboard");

            try
            {
                _logger.LogInformation("Inizio processo di login...");
                _logger.LogInformation($"ReturnUrl: {returnUrl}");

                // Verifica se i dati del form sono validi
                if (!ModelState.IsValid)
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"Errore ModelState: {error.ErrorMessage}");
                    }
                    return Page();
                }

                _logger.LogInformation($"Tentativo di login per l'utente: {Input.Username}");

                // Valida l'utente
                var user = await _authService.ValidateUserAsync(Input.Username, Input.Password);
                if (user == null)
                {
                    _logger.LogWarning($"Login fallito per l'utente: {Input.Username}. Credenziali non valide.");
                    ModelState.AddModelError(string.Empty, "Username o password non validi.");
                    return Page();
                }

                _logger.LogInformation($"Login riuscito per l'utente: {Input.Username}");

                // Esegui l'autenticazione
                await _authService.SignInAsync(user, Input.RememberMe);
                _logger.LogInformation($"Utente autenticato. Reindirizzamento a: {returnUrl}");

                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il login.");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore durante il login. Riprova più tardi.");
                return Page();
            }
        }

        public IActionResult OnGetTestRedirect()
        {
            // Questo metodo serve solo per testare i reindirizzamenti
            _logger.LogInformation("Test di reindirizzamento alla dashboard");
            return RedirectToPage("/Dashboard");
        }
    }
}