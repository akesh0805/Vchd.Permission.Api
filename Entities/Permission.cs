using System.ComponentModel.DataAnnotations;

namespace Vchd.Permission.Api.Entities;

public class Permission
{
    public int Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;
    [Required]
    public DateTime FromAt { get; set; }
    [Required]
    public DateTime UntillAt { get; set; }
    [Required]
    public EPermissionGive PermissionGive { get; set; }
    public EPermissionStatus Status {get;set;}
}

