using System;

namespace DotaDude.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; } // Aggiungiamo il campo per il tracciamento dell'ultimo login
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public string PreferredRole { get; set; } = "carry"; // Ruolo preferito del giocatore

    }
}