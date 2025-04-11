using System.ComponentModel.DataAnnotations;

namespace Vchd.Permission.Api.Models;

public class LoginModel
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }


