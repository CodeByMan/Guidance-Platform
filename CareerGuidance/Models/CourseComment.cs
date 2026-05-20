// CareerGuidance/Models/CourseComment.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CareerGuidance.Models
{
    public class CourseComment
    {
        public int CourseCommentId { get; set; }

        [Required]
        public int CourseId { get; set; }
        public Course Course { get; set; }

        [Required]
        public int UserId { get; set; }  // Foreign key to User
        public User User { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }  // Optional — set when the comment is edited

        public int? ParentCommentId { get; set; }
        public CourseComment ParentComment { get; set; }

        public ICollection<CourseComment> Replies { get; set; } = new List<CourseComment>();
    }
}
