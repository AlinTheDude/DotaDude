using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DotaDude.Models
{
    public class Hero
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LocalizedName { get; set; }
        public string PrimaryAttribute { get; set; }
        public string AttackType { get; set; }
        public List<string> Roles { get; set; }
        public string Img { get; set; }
        public string Icon { get; set; }
        public int BaseHealth { get; set; }
        public double BaseHealthRegen { get; set; }
        public int BaseMana { get; set; }
        public double BaseManaRegen { get; set; }
        public double BaseArmor { get; set; }
        public int BaseMr { get; set; }
        public int BaseAttackMin { get; set; }
        public int BaseAttackMax { get; set; }
        public int BaseStr { get; set; }
        public int BaseAgi { get; set; }
        public int BaseInt { get; set; }
        public double StrGain { get; set; }
        public double AgiGain { get; set; }
        public double IntGain { get; set; }
        public int AttackRange { get; set; }
        public int ProjectileSpeed { get; set; }
        public double AttackRate { get; set; }
        public int MoveSpeed { get; set; }
        public double Armor { get; set; }
        public string Lore { get; set; }


        // Calcola il percorso dell'immagine locale
        public string ImageUrl => $"/images/heroes/{Name.ToLower().Replace(" ", "_").Replace("-", "").Replace("'", "")}_full.png";

        public List<HeroAbility> Abilities { get; set; } = new List<HeroAbility>();
    }

    public class HeroAbility
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ManaCost { get; set; }
        public string Cooldown { get; set; }
        public bool IsUltimate { get; set; }
    }
}