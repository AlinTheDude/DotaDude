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
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly OpenDotaService _openDotaService;

        public IndexModel(ILogger<IndexModel> logger, OpenDotaService openDotaService)
        {
            _logger = logger;
            _openDotaService = openDotaService;
        }

        public async Task OnGetAsync()
        {
            // You could load featured heroes or recent matches here
            // For now, we'll keep it simple
        }
    }
}