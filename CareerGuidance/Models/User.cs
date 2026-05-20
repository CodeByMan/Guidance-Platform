namespace CareerGuidance.Models
{
    public partial class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Imagepath { get; set; } = "user.jpg";
        // Roles: 1 = Admin, 2 = Mentor, 3 = User
        public int RoleId { get; set; }
    }
        //public virtual ICollection<Event>? Events { get; set; }
        //public virtual MentorProfile? MentorProfile { get; set; }
}
