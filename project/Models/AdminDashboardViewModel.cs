namespace proje.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalGyms { get; set; }
        public int ActiveGyms { get; set; }
        public int TotalServices { get; set; }
        public int TotalTrainers { get; set; }
        public int ActiveTrainers { get; set; }
        public int TotalMembers { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int ApprovedAppointments { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<RecentAppointment> RecentAppointments { get; set; } = new();
        public List<RecentMember> RecentMembers { get; set; } = new();
    }

    public class RecentAppointment
    {
        public int Id { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string TrainerName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RecentMember
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}

