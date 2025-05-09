using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotaDude.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DotaDude.Pages
{
    public class MinigamesModel : PageModel
    {
        private readonly ILogger<MinigamesModel> _logger;
        public List<Minigame> Minigames { get; set; } = new List<Minigame>();

        public MinigamesModel(ILogger<MinigamesModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Populate with updated mini-games
            Minigames = new List<Minigame>
            {
                new Minigame
                {
                    Id = 1,
                    Name = "Hero Quiz",
                    Description = "Guess the hero from their ability descriptions. Test your knowledge of all 123+ heroes!",
                    GameType = "hero-quiz",
                    IsActive = true
                },
                new Minigame
                {
                    Id = 2,
                    Name = "Dota Trivia",
                    Description = "Test your knowledge about Dota 2 mechanics, tournaments, and the pro scene!",
                    GameType = "trivia-game",
                    IsActive = true
                },
                new Minigame
                {
                    Id = 3,
                    Name = "Item Guesser",
                    Description = "Can you identify Dota 2 items from their descriptions and components?",
                    GameType = "item-guesser-game",
                    IsActive = true
                }
            };
        }
    }
}