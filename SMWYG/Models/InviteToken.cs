using SMWYG.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class InviteToken
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public User Creator { get; set; } = null!;

    [Column("used_by")]
    public Guid? UsedBy { get; set; }

    [ForeignKey(nameof(UsedBy))]
    public User? User { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("max_uses")]
    public int MaxUses { get; set; } = 1;
}