using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManager.Models
{
    public class CounselorAssignment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SchoolId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? GradeId { get; set; }

        public Guid? GroupId { get; set; }

        [Required]
        public bool IsCounselor { get; set; } = true;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("SchoolId")]
        public virtual School School { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("GradeId")]
        public virtual GradeLevel? GradeLevel { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; }
    }
}
