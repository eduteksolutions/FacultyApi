using FacultyApi.model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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


       

[HttpPost("PunchIn")]
    public IActionResult PunchIn([FromBody] PunchInRequest model)
    {
        try
        {
            int result = 0;

            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            SqlCommand cmd = new SqlCommand(
                "InsertHRDCardAttendance", con);

            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@aDate", model.S_Date);
            cmd.Parameters.AddWithValue("@empCode", model.FacultyCd);
            cmd.Parameters.AddWithValue("@reason", model.Reason);
            cmd.Parameters.AddWithValue("@Status", model.Status);
            cmd.Parameters.AddWithValue("@facultyname", model.FacultyName);
            cmd.Parameters.AddWithValue("@latitude", model.Latitude);
            cmd.Parameters.AddWithValue("@longitude", model.Longitude);
            cmd.Parameters.AddWithValue("@inTime", model.EInTime);
            cmd.Parameters.AddWithValue("@outTime", DBNull.Value);
            cmd.Parameters.AddWithValue("@UserID", model.UserId);

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
            return StatusCode(500, new
            {
                Message = ex.Message
            });
        }
    }



    // POST: api/FacultyAttendance/PunchOut
    [HttpPost("PunchOut")]
    public IActionResult PunchOut([FromBody] PunchOutRequest model)
    {
        try
        {
            int result = 0;
            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            SqlCommand cmd = new SqlCommand(
                "UpdateHRDCardAttendanceOutTime", con);

            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@aDate", model.S_Date);
            cmd.Parameters.AddWithValue("@empCode", model.FacultyCd);
            cmd.Parameters.AddWithValue("@UserID", model.UserId);
            cmd.Parameters.AddWithValue("@outTime", model.EOutTime);

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
            return StatusCode(500, new
            {
                Message = ex.Message
            });
        }
    }

   
}
}