using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DotaDude.Models;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace DotaDude.Services
{
    public class OpenDotaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenDotaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<Hero>> GetHeroesAsync()
        {
            try
            {
                Console.WriteLine("Fetching heroes from OpenDota API...");

                var response = await _httpClient.GetAsync("heroStats");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Deserialize JSON from OpenDota API to a list of Hero objects
                var heroStatsRaw = JsonSerializer.Deserialize<List<JsonElement>>(content, options);
                var heroes = new List<Hero>();

                foreach (var item in heroStatsRaw)
                {
                    try
                    {
                        var hero = new Hero
                        {
                            Id = item.GetProperty("id").GetInt32(),
                            Name = item.GetProperty("name").GetString(),
                            LocalizedName = item.GetProperty("localized_name").GetString() ?? "Unnamed Hero",
                            PrimaryAttribute = item.GetProperty("primary_attr").GetString(),
                            AttackType = item.GetProperty("attack_type").GetString(),
                            Roles = JsonSerializer.Deserialize<List<string>>(item.GetProperty("roles").GetRawText(), options),
                            Img = $"https://api.opendota.com{item.GetProperty("img").GetString()}",
                            Icon = $"https://api.opendota.com{item.GetProperty("icon").GetString()}",
                            BaseHealth = item.GetProperty("base_health").GetInt32(),
                            BaseHealthRegen = item.GetProperty("base_health_regen").GetDouble(),
                            BaseMana = item.GetProperty("base_mana").GetInt32(),
                            BaseManaRegen = item.GetProperty("base_mana_regen").GetDouble(),
                            BaseArmor = item.GetProperty("base_armor").GetDouble(),
                            BaseMr = item.GetProperty("base_mr").GetInt32(),
                            BaseAttackMin = item.GetProperty("base_attack_min").GetInt32(),
                            BaseAttackMax = item.GetProperty("base_attack_max").GetInt32(),
                            BaseStr = item.GetProperty("base_str").GetInt32(),
                            StrGain = item.GetProperty("str_gain").GetDouble(),
                            BaseAgi = item.GetProperty("base_agi").GetInt32(),
                            AgiGain = item.GetProperty("agi_gain").GetDouble(),
                            BaseInt = item.GetProperty("base_int").GetInt32(),
                            IntGain = item.GetProperty("int_gain").GetDouble(),
                            AttackRange = item.GetProperty("attack_range").GetInt32(),
                            ProjectileSpeed = item.GetProperty("projectile_speed").GetInt32(),
                            AttackRate = item.GetProperty("attack_rate").GetDouble(),
                            MoveSpeed = item.GetProperty("move_speed").GetInt32(),
                            Armor = item.GetProperty("base_armor").GetDouble(),
                            Lore = "This is a placeholder for the hero's lore. In the full implementation, this would contain official lore."
                        };

                        heroes.Add(hero);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error mapping hero data: {ex.Message}");
                    }
                }

                Console.WriteLine($"Successfully fetched and mapped {heroes.Count} heroes.");
                return heroes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching heroes: {ex.Message}");
                return new List<Hero>();
            }
        }

        public async Task<Hero> GetHeroDetailsAsync(int heroId)
        {
            try
            {
                var heroes = await GetHeroesAsync();
                var hero = heroes.FirstOrDefault(h => h.Id == heroId);

                if (hero == null)
                {
                    Console.WriteLine($"Hero with ID {heroId} not found.");
                    return null;
                }

                Console.WriteLine($"Details fetched for Hero ID {heroId}: {hero.LocalizedName}");
                return hero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching hero details: {ex.Message}");
                return null;
            }
        }

        public async Task<List<HeroMatch>> GetHeroMatchesAsync(int heroId, int limit = 5)
        {
            try
            {
                var response = await _httpClient.GetAsync($"heroes/{heroId}/matches?limit={limit}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                // Parse matches and create a simplified list for display
                // This is a placeholder for demo purposes

                var matches = new List<HeroMatch>();
                for (int i = 0; i < limit; i++)
                {
                    matches.Add(new HeroMatch
                    {
                        MatchId = 6000000000 + i,
                        Win = i % 2 == 0,
                        Kills = new Random().Next(0, 15),
                        Deaths = new Random().Next(0, 10),
                        Assists = new Random().Next(0, 20),
                        Duration = $"{new Random().Next(20, 60)}:{new Random().Next(0, 60):D2}",
                        Date = DateTime.Now.AddDays(-i)
                    });
                }

                return matches;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching hero matches: {ex.Message}");
                return new List<HeroMatch>();
            }
        }

        public async Task<Match> GetMatchDetailsAsync(long matchId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"matches/{matchId}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var match = JsonSerializer.Deserialize<Match>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return match;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching match details: {ex.Message}");
                return null;
            }
        }

        // Helper methods
        private string GetHeroNameForUrl(string heroName)
        {
            return heroName.ToLower().Replace(" ", "_").Replace("-", "").Replace("'", "");
        }

        private void AddPlaceholderAbilities(Hero hero)
        {
            hero.Abilities = new List<HeroAbility>
            {
                new HeroAbility
                {
                    Id = 1,
                    Name = $"First Ability of {hero.Name}",
                    Description = "This is a placeholder for the hero's first ability description.",
                    ImageUrl = "https://cdn.dota2.com/apps/dota2/images/abilities/antimage_mana_break_md.png",
                    ManaCost = "50/60/70/80",
                    Cooldown = "15/12/9/6",
                    IsUltimate = false
                },
                new HeroAbility
                {
                    Id = 2,
                    Name = $"Second Ability of {hero.Name}",
                    Description = "This is a placeholder for the hero's second ability description.",
                    ImageUrl = "https://cdn.dota2.com/apps/dota2/images/abilities/antimage_blink_md.png",
                    ManaCost = "60",
                    Cooldown = "12",
                    IsUltimate = false
                },
                new HeroAbility
                {
                    Id = 3,
                    Name = $"Third Ability of {hero.Name}",
                    Description = "This is a placeholder for the hero's third ability description.",
                    ImageUrl = "https://cdn.dota2.com/apps/dota2/images/abilities/antimage_spell_shield_md.png",
                    ManaCost = "40",
                    Cooldown = "13",
                    IsUltimate = false
                },
                new HeroAbility
                {
                    Id = 4,
                    Name = $"Ultimate of {hero.Name}",
                    Description = "This is a placeholder for the hero's ultimate ability description.",
                    ImageUrl = "https://cdn.dota2.com/apps/dota2/images/abilities/antimage_mana_void_md.png",
                    ManaCost = "125/200/275",
                    Cooldown = "70/60/50",
                    IsUltimate = true
                }
            };
        }
    }
}