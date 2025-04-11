using System.ComponentModel.DataAnnotations;

namespace Vchd.Permission.Api.Models;

public class NewPermissionModel
{
    [Required]
    public string FullName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required]
    public DateTime FromAt { get; set; }
    [Required]
    public DateTime UntillAt { get; set; }
    [Required]
    public EPermissionGive PermissionGive { get; set; }
}
public enum EPermissionGive
{
    None = 0,
    Vchd = 1,
    VchdG=2,
    VchdZam=3,
    VchdKadr=4,
    VchdGlBux=5,
    VchdGlEko=6
}