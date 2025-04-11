using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Vchd.Permission.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Vchd.Permission.Api.Services;

public class AuthService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
{
    public async Task<IdentityResult> RegisterAsync(RegisterModel model)
    {
        var user = new IdentityUser { UserName = model.Username };
        var result = await userManager.CreateAsync(user, model.Password!);

        if (result.Succeeded)
        {
            if (await roleManager.RoleExistsAsync(model.Role.ToLower()))
                await userManager.AddToRoleAsync(user, model.Role.ToLower());
            else
                await userManager.AddToRoleAsync(user, "user");
        }
        return result;
    }
    public async Task<string?> LoginAsync(LoginModel model)
    {
        var user = await userManager.FindByNameAsync(model.Username);
        if (user == null || !await userManager.CheckPasswordAsync(user, model.Password))
            return null;

        var userRoles = await userManager.GetRolesAsync(user);
        var authClaims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName!),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

        authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = GenerateJwtToken(authClaims);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private JwtSecurityToken GenerateJwtToken(IEnumerable<Claim> claims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        return new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddHours(configuration.GetValue("JwtConfig:TokenValidityMins",3)),
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            
        );
    }
}

