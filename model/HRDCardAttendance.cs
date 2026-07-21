using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FacultyApi.model
{
   

         [Table("HRDCardAttendance")]
    public class HRDCardAttendance
    {
        [Key]

        public int empCode { get; set; }

        public DateTime aDate { get; set; }

        public string? Status { get; set; }

        public DateTime? inTime { get; set; }

        public string? eTimings { get; set; }

        public string? LeaveType { get; set; }

        public string? LoginName { get; set; }

        public DateTime? lUserDt { get; set; }

        public int UserID { get; set; }

        public string? reason { get; set; }

        public string? facultyName { get; set; }

        public string? ltype { get; set; }

        public string? longitude { get; set; }

        public string? latitude { get; set; }

        public DateTime? eintime { get; set; }

        public DateTime? outTime { get; set; }
    }
}

