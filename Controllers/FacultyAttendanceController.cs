using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacultyAttendanceController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FacultyAttendanceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // POST: api/FacultyAttendance/PunchIn
        [HttpPost("PunchIn")]
        public IActionResult PunchIn(
            int facultyCd,
            string s_date,
            string status,
            string reason,
            int userid,
            string facultyName,
            string longitude,
            string latitude,
            string eintime)
        {
            try
            {
                int result = 0;

                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                SqlCommand cmd = new SqlCommand(
                    "InsertHRDCardAttendance", con);

                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@aDate", s_date);
                cmd.Parameters.AddWithValue("@empCode", facultyCd);
                cmd.Parameters.AddWithValue("@reason", reason);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@facultyname", facultyName);
                cmd.Parameters.AddWithValue("@latitude", latitude);
                cmd.Parameters.AddWithValue("@longitude", longitude);
                cmd.Parameters.AddWithValue("@inTime", eintime);
                cmd.Parameters.AddWithValue("@outTime", DBNull.Value);
                cmd.Parameters.AddWithValue("@UserID", userid);

                con.Open();

                result = Convert.ToInt32(cmd.ExecuteScalar());

                return Ok(new
                {
                    Code = result == 1 ? "200" : "500",
                    Status = result == 1,
                    Message = result == 1
                        ? "Punch In Inserted"
                        : "Punch In Failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        // POST: api/FacultyAttendance/PunchOut
        [HttpPost("PunchOut")]
        public IActionResult PunchOut(
            int facultyCd,
            string s_date,
            int userid,
            string eouttime)
        {
            try
            {
                int result = 0;

                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                SqlCommand cmd = new SqlCommand(
                    "UpdateHRDCardAttendanceOutTime", con);

                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@aDate", s_date);
                cmd.Parameters.AddWithValue("@empCode", facultyCd);
                cmd.Parameters.AddWithValue("@UserID", userid);
                cmd.Parameters.AddWithValue("@outTime", eouttime);

                con.Open();

                result = Convert.ToInt32(cmd.ExecuteScalar());

                return Ok(new
                {
                    Code = result == 1 ? "200" : "500",
                    Status = result == 1,
                    Message = result == 1
                        ? "Punch Out Updated"
                        : "Punch Out Failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}