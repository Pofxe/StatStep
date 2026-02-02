using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StepikAnalytics.Backend.Models;

[Table("sync_runs")]
public class SyncRun
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("course_id")]
    public Guid CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "running";

    [Column("error_text")]
    public string? ErrorText { get; set; }

    [Column("fetched_items_count")]
    public int FetchedItemsCount { get; set; }
}
