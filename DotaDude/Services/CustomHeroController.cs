using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotaDude.Data;
using DotaDude.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotaDude.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomHeroController : ControllerBase
    {
        private readonly DotaDudeDbContext _context;

        public CustomHeroController(DotaDudeDbContext context)
        {
            _context = context;
        }

        // GET: api/CustomHero
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomHero>>> GetCustomHeroes(bool includeUnapproved = false)
        {
            // Default behavior: only return approved heroes unless specified
            if (!includeUnapproved)
            {
                return await _context.CustomHeroes
                    .Where(h => h.IsApproved && h.IsPublic)
                    .Include(h => h.Creator)
                    .Include(h => h.Abilities)
                    .ToListAsync();
            }

            // Admin view: include all heroes
            if (User.IsInRole("Admin"))
            {
                return await _context.CustomHeroes
                    .Include(h => h.Creator)
                    .Include(h => h.Abilities)
                    .ToListAsync();
            }

            // If the user is requesting unapproved heroes but isn't an admin
            return Forbid();
        }

        // GET: api/CustomHero/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomHero>> GetCustomHero(int id)
        {
            var customHero = await _context.CustomHeroes
                .Include(h => h.Creator)
                .Include(h => h.Abilities)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (customHero == null)
            {
                return NotFound();
            }

            // Check if user can view this hero
            bool canView = customHero.IsApproved && customHero.IsPublic;

            // Creator can always view their own hero
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null && customHero.CreatorUserId == user.Id)
                {
                    canView = true;
                }
            }

            // Admins can view all heroes
            if (User.IsInRole("Admin"))
            {
                canView = true;
            }

            if (!canView)
            {
                return Forbid();
            }

            return customHero;
        }

        // PUT: api/CustomHero/5/approve
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCustomHero(int id)
        {
            var customHero = await _context.CustomHeroes.FindAsync(id);
            if (customHero == null)
            {
                return NotFound();
            }

            customHero.IsApproved = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/CustomHero/5/reject
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectCustomHero(int id)
        {
            var customHero = await _context.CustomHeroes.FindAsync(id);
            if (customHero == null)
            {
                return NotFound();
            }

            customHero.IsApproved = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/CustomHero/user
        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CustomHero>>> GetUserCustomHeroes()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return await _context.CustomHeroes
                .Where(h => h.CreatorUserId == user.Id)
                .Include(h => h.Abilities)
                .ToListAsync();
        }
    }
}