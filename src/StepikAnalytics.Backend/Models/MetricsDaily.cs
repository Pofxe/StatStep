using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StepikAnalytics.Backend.Models;

[Table("metrics_daily")]
[PrimaryKey(nameof(CourseId), nameof(Date))]
public class MetricsDaily
{
    [Column("course_id")]
    public Guid CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("total_submissions")]
    public int TotalSubmissions { get; set; }

    [Column("correct_submissions")]
    public int CorrectSubmissions { get; set; }

    [Column("wrong_submissions")]
    public int WrongSubmissions { get; set; }

    [Column("new_learners")]
    public int NewLearners { get; set; }

    [Column("certificates")]
    public int Certificates { get; set; }

    [Column("reputation_delta")]
    public int ReputationDelta { get; set; }

    [Column("knowledge_delta")]
    public int KnowledgeDelta { get; set; }

    [Column("reviews_count")]
    public int ReviewsCount { get; set; }

    [Column("rating_value")]
    public double RatingValue { get; set; }

    [Column("rating_delta")]
    public double RatingDelta { get; set; }

    [Column("reviews_avg")]
    public double ReviewsAvg { get; set; }

    [Column("active_learners_dau")]
    public int ActiveLearnersDau { get; set; }
}
