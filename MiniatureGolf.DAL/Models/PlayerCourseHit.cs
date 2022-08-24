namespace MiniatureGolf.DAL.Models;

public class PlayerCourseHit
{
    public int PlayerId { get; set; }
    public Player Player { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; }

    public int? HitCount { get; set; }
}
