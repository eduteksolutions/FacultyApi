using FacultyApi.model;
using FacultyApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacultyNotificationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;

        public FacultyNotificationController(
            IConfiguration configuration,
            NotificationService notificationService)
        {
            _configuration = configuration;
            _notificationService = notificationService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] FacultyNotificationRequest request)
        {
            try
            {
                string? deviceToken = null;

                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                SqlCommand cmd = new SqlCommand(
                    @"SELECT DeviceToken
                      FROM HRDStaffMaster
                      WHERE UserID = @UserID",
                    con);

                cmd.Parameters.AddWithValue("@UserID", request.UserID);

                var result = await cmd.ExecuteScalarAsync();

                if (result != null)
                {
                    deviceToken = result.ToString();
                }

                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Faculty device token not found."
                    });
                }

                await _notificationService.SendMessageAsync(
                    deviceToken,
                    request.Title,
                    request.Message);

                return Ok(new
                {
                    success = true,
                    message = "Notification sent successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}