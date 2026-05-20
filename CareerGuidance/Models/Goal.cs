using System;
using System.Collections.Generic;

namespace CareerGuidance.Models
{
    public class Goal
    {
        public int GoalId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string GoalType { get; set; }
        public DateTime TargetDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<GoalStep> Steps { get; set; } = new List<GoalStep>();

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
