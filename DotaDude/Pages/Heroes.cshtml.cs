using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using DotaDude.Models;
using DotaDude.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DotaDude.Pages
{
    public class HeroesModel : PageModel
    {
        private readonly ILogger<HeroesModel> _logger;
        private readonly OpenDotaService _openDotaService;

        public List<Hero> Heroes { get; set; } = new List<Hero>();

        public HeroesModel(ILogger<HeroesModel> logger, OpenDotaService openDotaService)
        {
            _logger = logger;
            _openDotaService = openDotaService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Iniziando il recupero degli eroi con SSL bypass...");

                // Crea un handler che ignora i problemi di certificato SSL
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.BaseAddress = new Uri("https://api.opendota.com/api/");
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    // Fai la richiesta
                    _logger.LogInformation("Facendo richiesta diretta a https://api.opendota.com/api/heroStats");
                    var response = await httpClient.GetAsync("heroStats");
                    _logger.LogInformation($"Risposta ricevuta: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Contenuto ricevuto (lunghezza: {content.Length})");

                        if (!string.IsNullOrEmpty(content))
                        {
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            try
                            {
                                // Deserializza in JsonElement per mappare manualmente
                                var heroStatsRaw = JsonSerializer.Deserialize<JsonElement>(content, options);

                                if (heroStatsRaw.ValueKind == JsonValueKind.Array)
                                {
                                    Heroes = new List<Hero>();

                                    foreach (var item in heroStatsRaw.EnumerateArray())
                                    {
                                        try
                                        {
                                            var hero = new Hero
                                            {
                                                Id = item.GetProperty("id").GetInt32(),
                                                Name = item.GetProperty("name").GetString()?.Replace("npc_dota_hero_", ""),
                                                LocalizedName = item.GetProperty("localized_name").GetString(),
                                                PrimaryAttribute = item.GetProperty("primary_attr").GetString(),
                                                AttackType = item.GetProperty("attack_type").GetString(),
                                                Roles = JsonSerializer.Deserialize<List<string>>(item.GetProperty("roles").GetRawText(), options),

                                                // Statistiche di base
                                                BaseStr = item.GetProperty("base_str").GetInt32(),
                                                StrGain = item.GetProperty("str_gain").GetDouble(),
                                                BaseAgi = item.GetProperty("base_agi").GetInt32(),
                                                AgiGain = item.GetProperty("agi_gain").GetDouble(),
                                                BaseInt = item.GetProperty("base_int").GetInt32(),
                                                IntGain = item.GetProperty("int_gain").GetDouble(),
                                                BaseHealth = item.GetProperty("base_health").GetInt32(),
                                                BaseMana = item.GetProperty("base_mana").GetInt32(),
                                                BaseAttackMin = item.GetProperty("base_attack_min").GetInt32(),
                                                BaseAttackMax = item.GetProperty("base_attack_max").GetInt32(),
                                                MoveSpeed = item.GetProperty("move_speed").GetInt32(),
                                                BaseArmor = item.GetProperty("base_armor").GetDouble()
                                            };

                                            // Aggiungi abilità placeholder
                                            hero.Abilities = new List<HeroAbility>
                                            {
                                                new HeroAbility
                                                {
                                                    Id = 1,
                                                    Name = $"First Ability of {hero.LocalizedName}",
                                                    Description = "This is a placeholder for the hero's first ability description.",
                                                    ImageUrl = "/images/abilities/placeholder_ability.png",
                                                    ManaCost = "50/60/70/80",
                                                    Cooldown = "15/12/9/6",
                                                    IsUltimate = false
                                                },
                                                new HeroAbility
                                                {
                                                    Id = 2,
                                                    Name = $"Second Ability of {hero.LocalizedName}",
                                                    Description = "This is a placeholder for the hero's second ability description.",
                                                    ImageUrl = "/images/abilities/placeholder_ability.png",
                                                    ManaCost = "60",
                                                    Cooldown = "12",
                                                    IsUltimate = false
                                                },
                                                new HeroAbility
                                                {
                                                    Id = 3,
                                                    Name = $"Third Ability of {hero.LocalizedName}",
                                                    Description = "This is a placeholder for the hero's third ability description.",
                                                    ImageUrl = "/images/abilities/placeholder_ability.png",
                                                    ManaCost = "40",
                                                    Cooldown = "13",
                                                    IsUltimate = false
                                                },
                                                new HeroAbility
                                                {
                                                    Id = 4,
                                                    Name = $"Ultimate of {hero.LocalizedName}",
                                                    Description = "This is a placeholder for the hero's ultimate ability description.",
                                                    ImageUrl = "/images/abilities/placeholder_ability.png",
                                                    ManaCost = "125/200/275",
                                                    Cooldown = "70/60/50",
                                                    IsUltimate = true
                                                }
                                            };

                                            Heroes.Add(hero);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, $"Errore nella conversione di un eroe");
                                        }
                                    }

                                    _logger.LogInformation($"Convertiti con successo {Heroes.Count} eroi");
                                }
                                else
                                {
                                    _logger.LogWarning("La risposta JSON non è un array");
                                    ModelState.AddModelError(string.Empty, "I dati ricevuti dall'API non sono nel formato atteso.");
                                }
                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogError(jsonEx, "Errore nella deserializzazione JSON");
                                _logger.LogInformation($"Primi 500 caratteri del JSON: {content.Substring(0, Math.Min(500, content.Length))}");
                                ModelState.AddModelError(string.Empty, $"Errore nella lettura dei dati JSON: {jsonEx.Message}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Risposta API vuota");
                            ModelState.AddModelError(string.Empty, "L'API ha restituito una risposta vuota.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Risposta API non riuscita: {response.StatusCode}");
                        ModelState.AddModelError(string.Empty, $"L'API ha risposto con codice di stato {response.StatusCode}.");
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Errore nella richiesta HTTP");
                ModelState.AddModelError(string.Empty, $"Errore di connessione: {httpEx.Message}");
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Timeout della richiesta API");
                ModelState.AddModelError(string.Empty, "La richiesta all'API è scaduta. Riprova più tardi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generico durante il recupero degli eroi");
                ModelState.AddModelError(string.Empty, $"Si è verificato un errore: {ex.Message}");

                // Log dell'inner exception se presente
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception");
                    ModelState.AddModelError(string.Empty, $"Dettagli errore: {ex.InnerException.Message}");
                }
            }
        }
    }
}