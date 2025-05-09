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
    public class CustomHeroesModel : PageModel
    {
        private readonly DotaDudeDbContext _context;

        public CustomHeroesModel(DotaDudeDbContext context)
        {
            _context = context;
        }

        public List<CustomHero> CustomHeroes { get; set; } = new List<CustomHero>();

        public List<CustomHero> UserCustomHeroes { get; set; } = new List<CustomHero>();

        public List<string> AllRoles { get; } = new List<string>
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

        public async Task OnGetAsync()
        {
            // Get all approved and public custom heroes
            CustomHeroes = await _context.CustomHeroes
                .Where(h => h.IsApproved && h.IsPublic)
                .Include(h => h.Creator)
                .OrderByDescending(h => h.CreatedDate)
                .ToListAsync();

            // If user is logged in, get their custom heroes
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user != null)
                {
                    UserCustomHeroes = await _context.CustomHeroes
                        .Where(h => h.CreatorUserId == user.Id)
                        .OrderByDescending(h => h.CreatedDate)
                        .ToListAsync();
                }
            }
        }
    }
}