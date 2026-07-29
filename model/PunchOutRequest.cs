namespace FacultyApi.model
{
    public class PunchOutRequest
    {
        public int FacultyCd { get; set; }

        public DateTime S_Date { get; set; }

        public int UserId { get; set; }

        public string EOutTime { get; set; } = string.Empty;
    }
}
