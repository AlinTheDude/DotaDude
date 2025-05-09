using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotaDude.Models;
using DotaDude.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DotaDude.Pages
{
    public class HeroDetailsModel : PageModel
    {
        private readonly ILogger<HeroDetailsModel> _logger;
        private readonly OpenDotaService _openDotaService;

        public Hero Hero { get; set; }
        public List<HeroMatch> HeroMatches { get; set; } = new List<HeroMatch>();

        public HeroDetailsModel(ILogger<HeroDetailsModel> logger, OpenDotaService openDotaService)
        {
            _logger = logger;
            _openDotaService = openDotaService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Otteniamo i dettagli dell'eroe dal servizio
                Hero = await _openDotaService.GetHeroDetailsAsync(id);

                if (Hero == null)
                {
                    return NotFound();
                }

                // Opzionale: Recuperiamo le partite recenti dell'eroe
                HeroMatches = await _openDotaService.GetHeroMatchesAsync(id, 5);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching hero details for ID: {id}");
                return RedirectToPage("/Error");
            }
        }
    }
}