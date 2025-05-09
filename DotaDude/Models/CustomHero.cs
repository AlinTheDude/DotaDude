using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotaDude.Models
{
    public class CustomHero
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public string PrimaryAttribute { get; set; } // str, agi, int

        [Required]
        public string AttackType { get; set; } // Melee, Ranged

        [Required]
        public string Roles { get; set; } // Comma-separated roles

        [StringLength(1000)]
        public string Lore { get; set; }

        public string ImagePath { get; set; } // Optional: path to custom image

        // Base stats
        public int BaseHealth { get; set; } = 200;
        public double BaseHealthRegen { get; set; } = 0.5;
        public int BaseMana { get; set; } = 75;
        public double BaseManaRegen { get; set; } = 0.5;
        public double BaseArmor { get; set; } = 0;
        public int BaseMr { get; set; } = 25; // Base magic resistance

        // Attack stats
        public int BaseAttackMin { get; set; } = 40;
        public int BaseAttackMax { get; set; } = 45;
        public int AttackRange { get; set; } = 150; // Default for melee
        public double AttackRate { get; set; } = 1.7;

        // Attributes
        public int BaseStr { get; set; } = 19;
        public int BaseAgi { get; set; } = 19;
        public int BaseInt { get; set; } = 19;
        public double StrGain { get; set; } = 2.0;
        public double AgiGain { get; set; } = 2.0;
        public double IntGain { get; set; } = 2.0;

        // Movement
        public int MoveSpeed { get; set; } = 300;

        // Creator info
        public int CreatorUserId { get; set; }

        [ForeignKey("CreatorUserId")]
        public User Creator { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsPublic { get; set; } = false;
        public bool IsApproved { get; set; } = false;

        // Navigation property for abilities
        public List<CustomHeroAbility> Abilities { get; set; } = new List<CustomHeroAbility>();
    }

    public class CustomHeroAbility
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        public string ImagePath { get; set; }
        public string ManaCost { get; set; }
        public string Cooldown { get; set; }
        public bool IsUltimate { get; set; }

        // Foreign key for CustomHero
        public int CustomHeroId { get; set; }

        [ForeignKey("CustomHeroId")]
        public CustomHero CustomHero { get; set; }
    }
}