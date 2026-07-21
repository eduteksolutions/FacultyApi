namespace FacultyApi.model
{
    public class PunchInRequest
    {
        public int FacultyCd { get; set; }
        public DateTime S_Date { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public int UserId { get; set; }
        public string FacultyName { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string EInTime { get; set; }
    }
}
