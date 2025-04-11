using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vchd.Permission.Api.Data;
using Vchd.Permission.Api.Entities;
using Vchd.Permission.Api.Models;
using Vchd.Permission.Api.Services;

namespace Vchd.Permission.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController(AppDbContext context,
        UserManager<IdentityUser> userManager,
        TelegramBotService botService,
        ILogger<PermissionsController> logger) : ControllerBase
{

    private static readonly Dictionary<Entities.EPermissionGive, long> Approvers = new()
    {
        { Entities.EPermissionGive.Vchd, 282281724 },  // chaid boshliqlarniki
        { Entities.EPermissionGive.VchdG, 487049865 },
        { Entities.EPermissionGive.VchdZam, 43453160 },
        { Entities.EPermissionGive.VchdKadr, 466186109 },
        { Entities.EPermissionGive.VchdGlBux, 85833112 },
        { Entities.EPermissionGive.VchdGlEko, 319492124 }
    };
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Kadr")]
    public async Task<IActionResult> NewPermission([FromBody] NewPermissionModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var permission = new Entities.Permission
        {
            FullName = model.FullName,
            Description = model.Description,
            FromAt = model.FromAt.ToUniversalTime(),
            UntillAt = model.UntillAt.ToUniversalTime(),
            PermissionGive = (Entities.EPermissionGive)model.PermissionGive
        };
        context.Add(permission);
        await context.SaveChangesAsync();
        if (Approvers.TryGetValue((Entities.EPermissionGive)model.PermissionGive, out long chatId))
        {
            await botService.SendPermissionRequestAsync(permission, chatId);

            _ = Task.Delay(TimeSpan.FromMinutes(10)).ContinueWith(async _ =>
            {
                try
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var existingPermission = await db.Permissions.FindAsync(permission.Id);

                    if (existingPermission != null && existingPermission.Status == EPermissionStatus.Pending)
                    {
                        existingPermission.Status = EPermissionStatus.Timeout;
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обработке тайм-аута разрешения");
                }
            });
        }
        return Ok(permission);
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Kadr")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var permissions = await context.Permissions.ToListAsync();
        return Ok(permissions);
    }
}