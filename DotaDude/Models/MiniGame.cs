using System;
using System.Collections.Generic;

namespace DotaDude.Models
{
    public class Minigame
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string GameType { get; set; }
        public bool IsActive { get; set; }
    }

    public class HeroQuiz
    {
        public List<string> Abilities { get; set; } = new List<string>();
        public string CorrectHero { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CurrentQuestion { get; set; }
    }

    public class ItemMatch
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int MatchId { get; set; }
    }

    public class LastHitGame
    {
        public int Gold { get; set; }
        public int LastHits { get; set; }
        public int TotalCreeps { get; set; }
        public int TimeRemaining { get; set; }
    }
}