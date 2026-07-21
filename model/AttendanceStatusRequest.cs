namespace FacultyApi.model
{
    public class AttendanceStatusRequest
    {
        public int FacultyCd { get; set; }

        public int UserId { get; set; }

        public DateTime S_Date { get; set; }
    }
}
