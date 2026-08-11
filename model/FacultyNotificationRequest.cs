namespace FacultyApi.model
{
    public class FacultyNotificationRequest
    {
        public string? UserID { get; set; }
        public string? Code { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

