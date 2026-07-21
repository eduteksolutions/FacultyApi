using FacultyApi.model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UpdateDeviceTokenController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UpdateDeviceTokenController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult UpdateToken([FromBody] DeviceTokenModel model)
        {
            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            con.Open();

            SqlCommand cmd = new SqlCommand(
                "UPDATE HRDStaffMaster SET DeviceToken=@DeviceToken WHERE id=@FacultyCd  and UserID=@UserID",
                con);

            cmd.Parameters.AddWithValue("@FacultyCd", model.FacultyCd);

            cmd.Parameters.AddWithValue("@UserId", model.UserId);
            cmd.Parameters.AddWithValue("@DeviceToken", model.DeviceToken);

            cmd.ExecuteNonQuery();

            return Ok(new
            {
                Status = true,
                Message = "Device token updated successfully."
            });
        }
    }
}