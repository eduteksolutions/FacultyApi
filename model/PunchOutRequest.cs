namespace FacultyApi.model
{
    public class PunchOutRequest
    {
        public int FacultyCd { get; set; }

        public string S_Date { get; set; } = string.Empty;

        public int UserId { get; set; }

        public string EOutTime { get; set; } = string.Empty;
    }
}
