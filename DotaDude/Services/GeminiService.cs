using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace DotaDude.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly SemaphoreSlim _requestThrottler = new SemaphoreSlim(1, 1);
        private string _cachedPreferredModel;
        private DateTime _lastModelCheck = DateTime.MinValue;
        private Dictionary<string, string> _heroCommentCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly IMemoryCache _memoryCache;

        // Gestione avanzata rate limit
        private static DateTime _earliestNextRequestTime = DateTime.MinValue;
        private static readonly SemaphoreSlim _rateLimitLock = new SemaphoreSlim(1, 1);
        private static int _consecutiveErrors = 0;
        private static DateTime _lastErrorTime = DateTime.MinValue;

        // Impostazioni di throttling configurabili
        private readonly int _maxRequestsPerMinute;
        private readonly bool _useDynamicBackoff;
        private readonly TimeSpan _maxBackoffTime;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = configuration["GeminiApi:ApiKey"];
            _memoryCache = memoryCache;

            // Carica le impostazioni di configurazione
            _maxRequestsPerMinute = configuration.GetValue<int>("GeminiApi:MaxRequestsPerMinute", 60);
            _useDynamicBackoff = configuration.GetValue<bool>("GeminiApi:UseDynamicBackoff", true);
            _maxBackoffTime = TimeSpan.FromMinutes(configuration.GetValue<int>("GeminiApi:MaxBackoffMinutes", 5));

            // Inizializza la cache con alcuni commenti predefiniti
            InitializeCommentCache();
        }

        private void InitializeCommentCache()
        {
            // Aggiungi commenti predefiniti per gli eroi più popolari
            var fallbackComments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Axe", "Axe is a fearsome melee strength hero who excels at forcing enemies to attack him. His Berserker's Call taunts nearby foes, while Counter Helix deals damage when he's attacked. Culling Blade finishes low-health enemies with a devastating execution. Perfect for initiating fights and tanking damage." },
                { "Crystal Maiden", "Crystal Maiden is an intelligence support hero with powerful slowing and disabling abilities. Her Frostbite roots enemies, while Crystal Nova slows and damages in an area. Her global Arcane Aura provides mana regeneration to all allies, and her ultimate Freezing Field deals massive AOE damage." },
                { "Juggernaut", "Juggernaut is an agile carry hero who combines high damage output with survivability. His Blade Fury makes him spell immune while dealing damage, Healing Ward provides regeneration, and Omnislash is a devastating ultimate that delivers multiple strikes to random targets." },
                { "Pudge", "Pudge is a fearsome strength hero known for his infamous hook. Meat Hook pulls enemies toward him, while Rot deals AOE damage around him. Flesh Heap gives him passive magic resistance and strength, and Dismember is a powerful single-target disable that deals massive damage." },
                { "Invoker", "Invoker is one of the most complex and versatile heroes in Dota 2. With Quas, Wex, and Exort orbs, he can invoke up to 10 different spells. His high skill ceiling rewards mastery with powerful combinations of crowd control, damage, and utility effects." },
                { "Anti-Mage", "Anti-Mage is an agility carry who excels against magic-heavy lineups. With Blink for mobility, Mana Break to deplete enemies' mana, and Mana Void as a devastating ultimate that punishes high mana heroes, he's a nightmare for spellcasters." },
                { "Earthshaker", "Earthshaker is a strength initiator with powerful area stuns. His Echo Slam ultimate can devastate grouped enemies, while Fissure creates impassable terrain. Aftershock adds stun to all his abilities, making him excellent at controlling teamfights." },
                { "Shadow Fiend", "Shadow Fiend is a glass cannon who deals immense magical and physical damage. His Shadowraze spells provide burst damage, while Necromastery gives him souls for increased attack damage. His Requiem of Souls ultimate can turn teamfights with its massive AoE damage and fear effect." },
                { "Phantom Assassin", "Phantom Assassin is an agility carry known for massive critical strikes. Her Phantom Strike provides gap-closing, while Blur gives evasion against physical attacks. With Coup de Grace, she can one-shot heroes with lucky critical hits, making her one of the deadliest late-game carries." },
                { "Rubick", "Rubick is a versatile intelligence support known for his Spell Steal ultimate, which allows him to copy and use enemy spells. With Telekinesis for displacement and Fade Bolt for damage reduction, he's unpredictable and can turn enemies' strongest abilities against them." },
                // Aggiungiamo più eroi popolari per ridurre le chiamate API
                { "Legion Commander", "Legion Commander is a versatile strength hero who excels at single target duels. Her Duel ultimate forces an enemy hero to fight her, with the winner gaining permanent damage. With Press the Attack to purge debuffs and Overwhelming Odds to gain movement speed, she's effective at all stages of the game." },
                { "Lion", "Lion is a disruptive intelligence support known for his hex and mana drain abilities. Earth Spike provides line stun, while Hex transforms enemies into helpless critters. His ultimate Finger of Death deals massive single-target damage, making him feared throughout the game." },
                { "Sniper", "Sniper is a ranged agility carry who excels at dealing damage from a safe distance. With Headshot to slow enemies, Take Aim to extend his attack range, and Assassinate as a long-range finisher, he can be devastating if properly positioned." },
                { "Witch Doctor", "Witch Doctor is a versatile intelligence support with powerful healing and damage abilities. Paralyzing Cask bounces between enemies, Voodoo Restoration heals allies, and his Death Ward ultimate channels massive physical damage to nearby enemies." },
                { "Queen of Pain", "Queen of Pain is a mobile intelligence hero with high burst damage. Her Blink provides exceptional mobility, while Sonic Wave deals massive damage in a line. With Shadow Strike for DoT damage and Scream of Pain for AoE, she's lethal throughout the game." },
                { "Drow Ranger", "Drow Ranger is a ranged agility carry who excels at dealing physical damage. Her Frost Arrows slow enemies, while Multishot hits multiple targets. Her ultimate Marksmanship grants bonus agility and piercing attacks, making her devastating against low-armor heroes." },
                { "Void Spirit", "Void Spirit is a mobile intelligence hero who combines burst damage with elusive movement. His Aether Remnant controls enemies, Dissimilate lets him dodge attacks, and Astral Step provides quick positioning. He excels at picking off isolated targets." },
                { "Faceless Void", "Faceless Void is an agility carry known for his powerful Chronosphere ultimate, which freezes all units except himself in an area. Time Walk allows him to reverse damage, while Time Lock provides random bashes. He's most devastating in the late game." },
                { "Tiny", "Tiny is a versatile strength hero who grows stronger as he levels up. Avalanche and Toss provide powerful combos, while Tree Grab gives cleave damage. Initially played as a burst damage ganker, he transitions into a building-destroying monster in late game." },
                { "Lina", "Lina is a high-damage intelligence hero with powerful nukes. Dragon Slave damages in a line, Light Strike Array stuns in an area, and her Laguna Blade ultimate deals massive single-target damage. With Fiery Soul, she gains attack and movement speed when casting spells." }
            };

            // Aggiungi tutti i commenti al cache
            foreach (var entry in fallbackComments)
            {
                _heroCommentCache[entry.Key] = entry.Value;
            }
        }

        public async Task<List<string>> ListAvailableModelsAsync()
        {
            try
            {
                // Controlla se abbiamo già i modelli in cache
                if (_memoryCache.TryGetValue("AvailableModels", out List<string> cachedModels))
                {
                    _logger.LogInformation("Returning cached models list");
                    return cachedModels;
                }

                // Verifica se possiamo fare una nuova richiesta
                if (!await CanMakeRequestAsync())
                {
                    _logger.LogWarning("Rate limit reached when listing models");
                    return new List<string> { "models/gemini-1.5-pro" }; // Ritorna un valore predefinito
                }

                string baseUrl = _configuration["GeminiApi:BaseUrl"];
                string requestUrl = $"{baseUrl}models?key={_apiKey}";

                _logger.LogInformation("Listing available Gemini API models");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error when listing models: {response.StatusCode}. Response: {errorContent}");

                    // Gestisci il rate limiting
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        await HandleRateLimitingAsync(errorContent);
                    }

                    return new List<string> { "models/gemini-1.5-pro" }; // Ritorna un valore predefinito
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Available models response: {responseContent}");

                var models = new List<string>();

                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    if (doc.RootElement.TryGetProperty("models", out var modelsArray))
                    {
                        foreach (var model in modelsArray.EnumerateArray())
                        {
                            if (model.TryGetProperty("name", out var name))
                            {
                                models.Add(name.GetString());
                            }
                        }
                    }
                }

                // Resetta gli errori consecutivi dopo una richiesta riuscita
                await ResetConsecutiveErrorsAsync();

                // Salva in cache per 1 ora
                _memoryCache.Set("AvailableModels", models, TimeSpan.FromHours(1));

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing available models");
                await IncrementConsecutiveErrorsAsync();
                return new List<string> { "models/gemini-1.5-pro" }; // Ritorna un valore predefinito
            }
        }

        private async Task<string> GetPreferredModelAsync()
        {
            // Utilizziamo una cache del modello per evitare di chiamare l'API troppe volte
            if (_cachedPreferredModel != null && (DateTime.Now - _lastModelCheck).TotalHours < 1)
            {
                return _cachedPreferredModel;
            }

            if (_memoryCache.TryGetValue("PreferredModel", out string cachedModel))
            {
                _cachedPreferredModel = cachedModel;
                return cachedModel;
            }

            // Se stiamo avendo molti errori, usa direttamente un modello predefinito senza chiamare l'API
            if (_consecutiveErrors > 3)
            {
                _logger.LogWarning("Using default model due to consecutive errors");
                return "models/gemini-1.5-pro";
            }

            var availableModels = await ListAvailableModelsAsync();
            _logger.LogInformation($"Available models: {JsonSerializer.Serialize(availableModels)}");

            // Ordine di preferenza per i modelli
            string[] preferredModels = new[]
            {
                "models/gemini-1.5-pro",
                "models/gemini-1.5-flash",
                "models/gemini-2.0-flash",
                "models/gemini-1.5-pro-002",
                "models/gemini-1.5-pro-001"
            };

            foreach (var preferredModel in preferredModels)
            {
                if (availableModels.Contains(preferredModel))
                {
                    _cachedPreferredModel = preferredModel;
                    _lastModelCheck = DateTime.Now;
                    _logger.LogInformation($"Selected model: {_cachedPreferredModel}");

                    // Salva in cache per 1 ora
                    _memoryCache.Set("PreferredModel", _cachedPreferredModel, TimeSpan.FromHours(1));

                    return _cachedPreferredModel;
                }
            }

            // Se nessuno dei modelli preferiti è disponibile, prendi il primo che contiene "gemini" (esclusi quelli vision)
            foreach (var model in availableModels)
            {
                if (model.Contains("gemini") && !model.Contains("vision"))
                {
                    _cachedPreferredModel = model;
                    _lastModelCheck = DateTime.Now;
                    _logger.LogInformation($"Selected fallback model: {_cachedPreferredModel}");

                    // Salva in cache per 1 ora
                    _memoryCache.Set("PreferredModel", _cachedPreferredModel, TimeSpan.FromHours(1));

                    return _cachedPreferredModel;
                }
            }

            // Se proprio non troviamo nulla, usiamo un default
            _logger.LogWarning("No suitable Gemini model found, using default 'models/gemini-1.5-pro'");
            return "models/gemini-1.5-pro";
        }

        /// <summary>
        /// Controlla se possiamo fare una nuova richiesta basandosi sul rate limit
        /// </summary>
        private async Task<bool> CanMakeRequestAsync()
        {
            await _rateLimitLock.WaitAsync();
            try
            {
                // Controlla se dobbiamo aspettare in base a un precedente rate limit
                var now = DateTime.Now;
                if (now < _earliestNextRequestTime)
                {
                    var waitTime = _earliestNextRequestTime - now;
                    _logger.LogWarning($"Rate limit active. Must wait {waitTime.TotalSeconds:F1} seconds before next request.");
                    return false;
                }

                // Se abbiamo avuto troppi errori consecutivi, facciamo un backoff esponenziale
                if (_useDynamicBackoff && _consecutiveErrors > 0)
                {
                    // Backoff esponenziale basato sul numero di errori consecutivi
                    var backoffSeconds = Math.Min(Math.Pow(2, _consecutiveErrors), _maxBackoffTime.TotalSeconds);
                    var timeSinceLastError = (now - _lastErrorTime).TotalSeconds;

                    if (timeSinceLastError < backoffSeconds)
                    {
                        _logger.LogWarning($"Dynamic backoff active. Waiting after {_consecutiveErrors} consecutive errors. " +
                                         $"Must wait {backoffSeconds - timeSinceLastError:F1} more seconds.");
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                _rateLimitLock.Release();
            }
        }

        /// <summary>
        /// Estrae il tempo di retry suggerito dal messaggio di errore e imposta il rate limit
        /// </summary>
        private async Task HandleRateLimitingAsync(string errorResponse)
        {
            await _rateLimitLock.WaitAsync();
            try
            {
                // Incrementa il contatore di errori consecutivi
                _consecutiveErrors++;
                _lastErrorTime = DateTime.Now;

                // Prova a estrarre il tempo di retry dall'errore
                TimeSpan retryDelay = ExtractRetryDelay(errorResponse);

                // Aggiungi un po' di buffer per sicurezza (10% in più)
                retryDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * 1.1);

                // Assicurati che non sia troppo breve
                if (retryDelay.TotalSeconds < 5)
                {
                    retryDelay = TimeSpan.FromSeconds(5); // Minimo 5 secondi
                }

                // Calcola quando possiamo fare la prossima richiesta
                _earliestNextRequestTime = DateTime.Now.Add(retryDelay);

                _logger.LogWarning($"Rate limit hit. Next request allowed after: {_earliestNextRequestTime} (delay: {retryDelay.TotalSeconds:F1}s)");
            }
            finally
            {
                _rateLimitLock.Release();
            }
        }

        private async Task IncrementConsecutiveErrorsAsync()
        {
            await _rateLimitLock.WaitAsync();
            try
            {
                _consecutiveErrors++;
                _lastErrorTime = DateTime.Now;
                _logger.LogWarning($"Incremented consecutive errors to {_consecutiveErrors}");
            }
            finally
            {
                _rateLimitLock.Release();
            }
        }

        private async Task ResetConsecutiveErrorsAsync()
        {
            await _rateLimitLock.WaitAsync();
            try
            {
                if (_consecutiveErrors > 0)
                {
                    _logger.LogInformation($"Reset consecutive errors from {_consecutiveErrors} to 0");
                    _consecutiveErrors = 0;
                }
            }
            finally
            {
                _rateLimitLock.Release();
            }
        }

        /// <summary>
        /// Estrae il tempo di retry dal messaggio di errore dell'API
        /// </summary>
        private TimeSpan ExtractRetryDelay(string errorResponse)
        {
            try
            {
                // Cerca il pattern "retryDelay": "Xs" nel JSON
                var match = Regex.Match(errorResponse, @"""retryDelay"":\s*""(\d+)s""");
                if (match.Success && match.Groups.Count > 1)
                {
                    if (int.TryParse(match.Groups[1].Value, out int seconds))
                    {
                        return TimeSpan.FromSeconds(seconds);
                    }
                }

                // Se non troviamo il pattern o non riusciamo a fare il parsing, usiamo un valore predefinito
                return TimeSpan.FromSeconds(30);
            }
            catch
            {
                // In caso di errori nel parsing, usa un valore predefinito
                return TimeSpan.FromSeconds(30);
            }
        }

        public async Task<string> GetGenericCommentAsync(string heroName)
        {
            try
            {
                // Normalizza il nome dell'eroe (rimuovi spazi extra, converti a lowercase per confronti case-insensitive)
                heroName = heroName?.Trim();
                if (string.IsNullOrEmpty(heroName))
                {
                    _logger.LogWarning("Empty hero name provided");
                    return "Hero information not available";
                }

                // Controlla se il commento è già in cache
                string cacheKey = $"hero_comment_{heroName.ToLowerInvariant()}";

                // Controlla prima la cache in-memory (per maggiore efficienza)
                if (_memoryCache.TryGetValue(cacheKey, out string cachedMemoryComment))
                {
                    _logger.LogInformation($"Returning in-memory cached comment for hero: {heroName}");
                    return cachedMemoryComment;
                }

                // Controlla la cache del dizionario interno
                if (_heroCommentCache.TryGetValue(heroName, out var cachedComment))
                {
                    _logger.LogInformation($"Returning dictionary cached comment for hero: {heroName}");

                    // Salva anche nella cache in-memory per accesso più veloce
                    _memoryCache.Set(cacheKey, cachedComment, TimeSpan.FromDays(7));

                    return cachedComment;
                }

                // Verifica se possiamo fare una nuova richiesta API
                if (!await CanMakeRequestAsync())
                {
                    _logger.LogWarning($"Rate limit active for hero comment: {heroName}. Using fallback.");
                    string fallbackComment = GetFallbackComment(heroName);

                    // Salva il commento fallback nella cache per evitare richieste future
                    _memoryCache.Set(cacheKey, fallbackComment, TimeSpan.FromHours(1));
                    _heroCommentCache[heroName] = fallbackComment;

                    return fallbackComment;
                }

                // Usa un semaforo per limitare le richieste concorrenti
                await _requestThrottler.WaitAsync();

                try
                {
                    // Ottieni il modello preferito
                    string modelToUse = await GetPreferredModelAsync();

                    // Costruisci l'URL per l'API Gemini
                    string baseUrl = _configuration["GeminiApi:BaseUrl"];
                    string requestUrl = $"{baseUrl}{modelToUse}:generateContent?key={_apiKey}";

                    _logger.LogInformation($"Making request to Gemini API for hero: {heroName} using model: {modelToUse}");

                    // Corpo della richiesta
                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new
                                    {
                                        text = $"Write a short interesting comment about the Dota 2 hero {heroName}. Include a brief description of their abilities and role in the game. Keep it under 100 words."
                                    }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            maxOutputTokens = 200,
                            topP = 0.8,
                            topK = 40
                        }
                    };

                    _logger.LogDebug($"Request URL: {requestUrl}");
                    _logger.LogDebug($"Request Body: {JsonSerializer.Serialize(requestBody)}");

                    // Invia la richiesta all'API Gemini
                    var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Gemini API error: {response.StatusCode}. Response: {errorContent}");

                        // Gestione specifica per rate limit
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            await HandleRateLimitingAsync(errorContent);
                            _logger.LogWarning("Rate limit reached. Returning fallback comment.");
                            string fallbackComment = GetFallbackComment(heroName);

                            // Salva il commento fallback nella cache temporaneamente
                            _memoryCache.Set(cacheKey, fallbackComment, TimeSpan.FromHours(1));
                            _heroCommentCache[heroName] = fallbackComment;

                            return fallbackComment;
                        }

                        // Incrementa il contatore di errori per altri tipi di errori
                        await IncrementConsecutiveErrorsAsync();

                        return $"Impossibile generare commento per {heroName}. Errore API: {response.StatusCode}";
                    }

                    // Analizza la risposta dell'API Gemini
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Gemini API Response: {responseContent}");

                    // Resetta gli errori consecutivi dopo una richiesta riuscita
                    await ResetConsecutiveErrorsAsync();

                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                            candidates.GetArrayLength() > 0 &&
                            candidates[0].TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0 &&
                            parts[0].TryGetProperty("text", out var text))
                        {
                            string comment = text.GetString();

                            // Salva nella cache
                            _heroCommentCache[heroName] = comment;

                            // Salva anche nella cache in-memory per 7 giorni
                            _memoryCache.Set(cacheKey, comment, TimeSpan.FromDays(7));

                            _logger.LogInformation($"Cached comment for hero: {heroName}");
                            return comment;
                        }
                        else
                        {
                            _logger.LogWarning("Unexpected Gemini API response format");
                            await IncrementConsecutiveErrorsAsync();
                            string fallbackComment = GetFallbackComment(heroName);

                            // Salva il commento fallback nelle cache
                            _memoryCache.Set(cacheKey, fallbackComment, TimeSpan.FromHours(1));
                            _heroCommentCache[heroName] = fallbackComment;

                            return fallbackComment;
                        }
                    }
                }
                finally
                {
                    // Rilascia il semaforo
                    _requestThrottler.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching comments for hero: {heroName}");
                await IncrementConsecutiveErrorsAsync();
                return GetFallbackComment(heroName);
            }
        }

        // Metodo per generare un commento di fallback quando l'API non è disponibile
        private string GetFallbackComment(string heroName)
        {
            // Alcuni commenti predefiniti per eroi popolari (già aggiunti all'inizializzazione)

            // Se abbiamo un commento predefinito per questo eroe, usalo
            if (_heroCommentCache.ContainsKey(heroName))
            {
                return _heroCommentCache[heroName];
            }

            // Per eroi meno conosciuti, generiamo un commento generico
            return $"{heroName} is a powerful hero in Dota 2 with unique abilities and strategic gameplay. To see detailed information about {heroName}'s abilities and playstyle, check the official Dota 2 wiki. Comments from AI are temporarily unavailable due to API rate limits.";
        }
    }
}