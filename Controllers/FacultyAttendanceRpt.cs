using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FacultyApi.Controllers
{
 
 
        [ApiController]
        [Route("api/[controller]")]
        public class FacultyAttendanceRptController : ControllerBase
        {
            private readonly IConfiguration _configuration;

            public FacultyAttendanceRptController(IConfiguration configuration)
            {
                _configuration = configuration;
            }


            [HttpGet("FacultyAttendanceReport")]
            public IActionResult FacultyAttendanceReport(
                DateTime aDate,
                int empCode,
                int searchIndex,
                string loginName,
                int userId)
            {
                try
                {
                    List<object> list = new();


                    using SqlConnection con = new SqlConnection(
                        _configuration.GetConnectionString("DefaultConnection")
                    );


                    using SqlCommand cmd = new SqlCommand(
                        "edu.GetHRDStaffAttendanceRpt",
                        con
                    );


                    cmd.CommandType = CommandType.StoredProcedure;


                    cmd.Parameters.AddWithValue("@aDate", aDate);
                    cmd.Parameters.AddWithValue("@EmpCode", empCode);
                    cmd.Parameters.AddWithValue("@SerachIndex", searchIndex);
                    cmd.Parameters.AddWithValue("@LoginName", loginName);
                    cmd.Parameters.AddWithValue("@UserID", userId);


                    con.Open();


                    using SqlDataReader dr = cmd.ExecuteReader();


                    while (dr.Read())
                    {
                        list.Add(new
                        {
                            FacultyName = dr["facultyName"]?.ToString(),
                            Status = dr["Status"]?.ToString(),
                            InTime = dr["inTime"]?.ToString(),
                            OutTime = dr["outTime"]?.ToString(),
                            Reason = dr["reason"]?.ToString(),
                            Latitude = dr["latitude"]?.ToString(),
                            Longitude = dr["longitude"]?.ToString(),
                            AttendanceDate = dr["aDate"] == DBNull.Value
                                ? ""
                                : Convert.ToDateTime(dr["aDate"])
                                    .ToString("yyyy-MM-dd")
                        });
                    }


                    return Ok(list);
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Error = ex.Message
                    });
                }
            }
        }
    }

