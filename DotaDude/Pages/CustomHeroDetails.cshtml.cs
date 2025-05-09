using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotaDude.Data;
using DotaDude.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DotaDude.Pages
{
    public class CustomHeroDetailsModel : PageModel
    {
        private readonly DotaDudeDbContext _context;

        public CustomHeroDetailsModel(DotaDudeDbContext context)
        {
            _context = context;
        }

        public CustomHero Hero { get; set; }
        public List<CustomHeroAbility> Abilities { get; set; } = new List<CustomHeroAbility>();
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Fetch the hero with abilities and creator
            Hero = await _context.CustomHeroes
                .Include(h => h.Creator)
                .Include(h => h.Abilities)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (Hero == null)
            {
                return NotFound();
            }

            // Sort abilities so the Ultimate is last
            Abilities = Hero.Abilities.OrderBy(a => a.IsUltimate).ToList();

            // Check if the current user is the owner
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user != null)
                {
                    IsOwner = (Hero.CreatorUserId == user.Id);
                    IsAdmin = User.IsInRole("Admin");  // Assumendo che ci sia un sistema di ruoli
                }
            }

            // Check if user can view this hero
            bool canView = Hero.IsApproved && Hero.IsPublic;

            // Creator and admins can always view
            if (IsOwner || IsAdmin)
            {
                canView = true;
            }

            if (!canView)
            {
                ErrorMessage = "Questo eroe è in attesa di approvazione o non è pubblico";
                return Page();
            }

            return Page();
        }
    }
}