using FacultyApi.model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

//using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FacultyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultyActivityController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public FacultyActivityController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("status")]
        public IActionResult CheckTeacher([FromBody] FacultyActivityRequest model)
        {
            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            con.Open();

            SqlCommand cmd = new SqlCommand(@"
                SELECT IsActive
                FROM HRDStaffMaster
                WHERE id = @FacultyCd
                  AND UserID = @UserID
            ", con);

            cmd.Parameters.AddWithValue("@FacultyCd", model.FacultyCd);
            cmd.Parameters.AddWithValue("@UserID", model.UserId);

            object? result = cmd.ExecuteScalar();

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    active = false,
                    message = "faculty not found."
                });
            }

            bool isActive = Convert.ToBoolean(result);

            return Ok(new
            {
                success = true,
                active = isActive,
                message = isActive
                    ? "faculty is active."
                    : "faculty is inactive."
            });
        }
    }
}
