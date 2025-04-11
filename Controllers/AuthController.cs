using Vchd.Permission.Api.Models;
using Vchd.Permission.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Vchd.Permission.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(AuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await authService.RegisterAsync(model);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("User registered successfully!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var token = await authService.LoginAsync(model);
            if (token == null)
                return Unauthorized("Invalid username or password");

            return Ok(new { Token = token });
        }
    }
}
