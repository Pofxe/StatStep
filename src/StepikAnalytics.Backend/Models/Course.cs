using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StepikAnalytics.Backend.Models;

[Table("courses")]
public class Course
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("stepik_course_id")]
    public int StepikCourseId { get; set; }

    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("cover_url")]
    [MaxLength(1000)]
    public string? CoverUrl { get; set; }

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_synced_at")]
    public DateTime? LastSyncedAt { get; set; }

    public ICollection<SyncRun> SyncRuns { get; set; } = new List<SyncRun>();
    public ICollection<MetricsDaily> DailyMetrics { get; set; } = new List<MetricsDaily>();
}
