using CareerGuidance.Models;
using CodingLanguageQuiz.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerGuidance.Models
{
    public partial class CareerDbContext : DbContext
    {
        public CareerDbContext(DbContextOptions<CareerDbContext> options) : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; } = null!;
        public DbSet<Goal> Goals { get; set; } = null!;
        public DbSet<GoalStep> GoalSteps { get; set; } = null!;
        public DbSet<Resume> Resumes { get; set; } = null!;
        public DbSet<Job> Jobs { get; set; } = null!;
        public DbSet<JobApplication> JobApplications { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<EventApplication> EventApplications { get; set; } = null!;
        public DbSet<SavedJob> SavedJobs { get; set; } = null!;
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<CourseComment> CourseComments { get; set; } = null!;
        public DbSet<MentorRequest> MentorRequests { get; set; } = null!;
        public DbSet<CareerAssessment> CareerAssessments { get; set; } = null!;
        public DbSet<MentorProfile> MentorProfiles { get; set; } = null!;
        public DbSet<CareerPath> CareerPaths { get; set; } = null!;
        public DbSet<JobRole> JobRoles { get; set; } = null!;
        public DbSet<Achievement> Achievements { get; set; } = null!;
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
        public DbSet<QuizResult> QuizResults { get; set; } = null!;
        public DbSet<ContactMessage> ContactMessages { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId).HasName("PK__user__3214EC07D92FFF79");

                entity.ToTable("user");

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .HasColumnName("email");

                entity.Property(e => e.Password)
                    .HasMaxLength(100)
                    .HasColumnName("password");

                entity.Property(e => e.RoleId)
                    .HasDefaultValueSql("((1))")
                    .HasColumnName("roleId");

                entity.Property(e => e.Username)
                    .HasMaxLength(70)
                    .HasColumnName("username");
            });

            modelBuilder.Entity<Goal>(entity =>
            {
                entity.HasKey(g => g.GoalId);

                entity.Property(g => g.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(g => g.Description)
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventId);
            });

            modelBuilder.Entity<EventApplication>(entity =>
            {
                entity.HasKey(ea => ea.EventApplicationId);
                entity.HasOne(ea => ea.User)
                      .WithMany()
                      .HasForeignKey(ea => ea.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ea => ea.Event)
                      .WithMany()
                      .HasForeignKey(ea => ea.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });



            modelBuilder.Entity<MentorRequest>(entity =>
            {
                entity.HasKey(e => e.MentorRequestId);

                entity.HasOne(e => e.Mentor)
                    .WithMany()
                    .HasForeignKey(e => e.MentorId)
                    .OnDelete(DeleteBehavior.Restrict); // NO CASCADE here!

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade); // You can keep cascade here or switch both to Restrict
            });

            {
                modelBuilder.Entity<CourseComment>()
                    .HasOne(c => c.ParentComment)
                    .WithMany(p => p.Replies)
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity<CourseComment>()
                    .HasOne(c => c.Course)
                    .WithMany(c => c.CourseComments)
                    .HasForeignKey(c => c.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<CourseComment>()
                    .HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            };

            modelBuilder.Entity<Job>()
                .Property(j => j.PublishDate)
                .HasColumnType("date");

            modelBuilder.Entity<Job>()
                .Property(j => j.Deadline)
                .HasColumnType("date");

            // Similarly for other entities referencing User, e.g.:
            modelBuilder.Entity<JobApplication>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);  // or NoAction
            });

            modelBuilder.Entity<Resume>(entity =>
            {
                entity.HasKey(r => r.ResumeId);

                entity.Property(r => r.FullName)
                    .IsRequired();

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
