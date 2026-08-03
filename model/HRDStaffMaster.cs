using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FacultyApi.model
{
    
        [Table("HRDStaffMaster")]
        public class HRDStaffMaster
        {
            [Key]
            public int id { get; set; }

            public int code { get; set; }

            public string? sName { get; set; }

            public string? Designation { get; set; }

            public int UserID { get; set; }

            public string? DeviceToken { get; set; }

            public string? DeviceType { get; set; }

            public string? EmailID { get; set; }

            public bool? isActive { get; set; }
        }
    
}
