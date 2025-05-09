using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DotaDude.Data;
using DotaDude.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DotaDude.Pages
{
    [Authorize] // Only logged-in users can access this page
    public class CreateCustomHeroModel : PageModel
    {
        private readonly DotaDudeDbContext _context;

        public CreateCustomHeroModel(DotaDudeDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CustomHero CustomHero { get; set; }

        [BindProperty]
        public List<string> SelectedRoles { get; set; } = new List<string>();

        [BindProperty]
        public List<CustomHeroAbility> Abilities { get; set; } = new List<CustomHeroAbility>();

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        // List of available roles for heroes in Dota 2
        public List<string> AvailableRoles { get; } = new List<string>
        {
            "Carry",
            "Support",
            "Nuker",
            "Disabler",
            "Jungler",
            "Durable",
            "Escape",
            "Pusher",
            "Initiator"
        };

        public void OnGet()
        {
            // Initialize default hero values
            CustomHero = new CustomHero
            {
                BaseHealth = 200,
                BaseHealthRegen = 0.5,
                BaseMana = 75,
                BaseManaRegen = 0.5,
                BaseArmor = 0,
                BaseMr = 25,
                BaseAttackMin = 40,
                BaseAttackMax = 45,
                AttackRange = 150,
                AttackRate = 1.7,
                BaseStr = 19,
                BaseAgi = 19,
                BaseInt = 19,
                StrGain = 2.0,
                AgiGain = 2.0,
                IntGain = 2.0,
                MoveSpeed = 300,
                AttackType = "Melee",   // Default to Melee
                PrimaryAttribute = "str", // Default to Strength
                IsPublic = true  // Default to public
            };

            // Initialize empty abilities list
            Abilities = new List<CustomHeroAbility>();
            for (int i = 0; i < 4; i++)
            {
                Abilities.Add(new CustomHeroAbility
                {
                    IsUltimate = (i == 3)
                });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    ErrorMessage = "Please fix the errors in the form";
                    return Page();
                }

                // Validate roles selection
                if (SelectedRoles == null || !SelectedRoles.Any())
                {
                    ModelState.AddModelError("SelectedRoles", "Please select at least one role for your hero");
                    ErrorMessage = "Please select at least one role for your hero";
                    return Page();
                }

                // Validate attack values
                if (CustomHero.BaseAttackMin >= CustomHero.BaseAttackMax)
                {
                    ModelState.AddModelError("CustomHero.BaseAttackMax", "Maximum attack must be greater than minimum attack");
                    ErrorMessage = "Maximum attack must be greater than minimum attack";
                    return Page();
                }

                // Default attack range if not set properly
                if (CustomHero.AttackType == "Melee" && (CustomHero.AttackRange < 150 || CustomHero.AttackRange > 200))
                {
                    CustomHero.AttackRange = 150;
                }
                else if (CustomHero.AttackType == "Ranged" && (CustomHero.AttackRange < 400 || CustomHero.AttackRange > 700))
                {
                    CustomHero.AttackRange = 550;
                }

                // Get current logged-in user
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    ErrorMessage = "User not found. Please log in again.";
                    return Page();
                }

                // Set hero creator and creation date
                CustomHero.CreatorUserId = user.Id;
                CustomHero.CreatedDate = DateTime.Now;
                CustomHero.IsApproved = false; // New heroes are not approved by default

                // Combine selected roles
                CustomHero.Roles = string.Join(",", SelectedRoles);

                // Process abilities from form
                List<CustomHeroAbility> heroAbilities = new List<CustomHeroAbility>();
                bool missingAbilities = false;

                for (int i = 0; i < 4; i++)
                {
                    var abilityName = Request.Form[$"Abilities[{i}].Name"].ToString();
                    var abilityDesc = Request.Form[$"Abilities[{i}].Description"].ToString();
                    var manaCost = Request.Form[$"Abilities[{i}].ManaCost"].ToString();
                    var cooldown = Request.Form[$"Abilities[{i}].Cooldown"].ToString();
                    var isUltimate = i == 3;

                    if (string.IsNullOrWhiteSpace(abilityName) || string.IsNullOrWhiteSpace(abilityDesc))
                    {
                        missingAbilities = true;
                        ErrorMessage = $"Please provide a name and description for {(isUltimate ? "Ultimate" : $"Ability {i + 1}")}";
                        break;
                    }

                    heroAbilities.Add(new CustomHeroAbility
                    {
                        Name = abilityName,
                        Description = abilityDesc,
                        ManaCost = manaCost,
                        Cooldown = cooldown,
                        IsUltimate = isUltimate
                    });
                }

                if (missingAbilities)
                {
                    // Pass the abilities back to the view
                    Abilities = heroAbilities;
                    return Page();
                }

                // Add hero to database context first to get ID
                await _context.CustomHeroes.AddAsync(CustomHero);
                await _context.SaveChangesAsync();

                // Now assign the hero ID to each ability and save them
                foreach (var ability in heroAbilities)
                {
                    ability.CustomHeroId = CustomHero.Id;
                    await _context.CustomHeroAbilities.AddAsync(ability);
                }

                await _context.SaveChangesAsync();

                SuccessMessage = $"Your hero '{CustomHero.Name}' has been created successfully! It will be visible to other users once approved by an admin.";

                // Reset for new hero creation
                CustomHero = new CustomHero
                {
                    BaseHealth = 200,
                    BaseHealthRegen = 0.5,
                    BaseMana = 75,
                    BaseManaRegen = 0.5,
                    BaseArmor = 0,
                    BaseMr = 25,
                    BaseAttackMin = 40,
                    BaseAttackMax = 45,
                    AttackRange = 150,
                    AttackRate = 1.7,
                    BaseStr = 19,
                    BaseAgi = 19,
                    BaseInt = 19,
                    StrGain = 2.0,
                    AgiGain = 2.0,
                    IntGain = 2.0,
                    MoveSpeed = 300,
                    AttackType = "Melee",
                    PrimaryAttribute = "str",
                    IsPublic = true
                };

                // Initialize empty abilities again
                Abilities = new List<CustomHeroAbility>();
                for (int i = 0; i < 4; i++)
                {
                    Abilities.Add(new CustomHeroAbility
                    {
                        IsUltimate = (i == 3)
                    });
                }

                SelectedRoles = new List<string>();

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while saving your hero: {ex.Message}";
                return Page();
            }
        }
    }
}