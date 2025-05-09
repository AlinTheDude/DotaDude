using System;
using System.Collections.Generic;

namespace DotaDude.Models
{
    public class Match
    {
        public long MatchId { get; set; }
        public bool RadiantWin { get; set; }
        public DateTime StartTime { get; set; }
        public long Duration { get; set; }
        public int GameMode { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();

        // Formatted time for display
        public string FormattedDuration
        {
            get
            {
                TimeSpan time = TimeSpan.FromSeconds(Duration);
                return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
            }
        }
    }

    public class Player
    {
        public long AccountId { get; set; }
        public int HeroId { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int LastHits { get; set; }
        public int Denies { get; set; }
        public int GoldPerMin { get; set; }
        public int XpPerMin { get; set; }
        public int Level { get; set; }
        public int TeamNumber { get; set; }
        public bool IsRadiant => TeamNumber == 0;

        // Items
        public int Item0 { get; set; }
        public int Item1 { get; set; }
        public int Item2 { get; set; }
        public int Item3 { get; set; }
        public int Item4 { get; set; }
        public int Item5 { get; set; }

        // KDA ratio
        public double KDA
        {
            get
            {
                if (Deaths == 0)
                    return Kills + Assists;
                return Math.Round((Kills + Assists) / (double)Deaths, 2);
            }
        }
    }

    public class HeroMatch
    {
        public long MatchId { get; set; }
        public bool Win { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string Duration { get; set; }
        public DateTime Date { get; set; }
    }
}