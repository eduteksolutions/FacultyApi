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
        [HttpGet("version")]
        public IActionResult Version()
        {
            return Ok(new
            {
                version = "FacultyNotificationController-v2",
                time = DateTime.Now
            });
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] FacultyNotificationRequest request)
        {
            try
            {
                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                SqlCommand cmd = new SqlCommand(
                    @"SELECT DeviceToken
          FROM HRDStaffMaster
          WHERE UserID = @UserID
          AND DeviceToken IS NOT NULL",
                    con);

                cmd.Parameters.AddWithValue("@UserID", request.UserID);


                List<string> deviceTokens = new List<string>();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string? token = reader["DeviceToken"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        deviceTokens.Add(token);
                    }
                }


                if (deviceTokens.Count == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No faculty device tokens found."
                    });
                }


                foreach (var token in deviceTokens)
                {
                    await _notificationService.SendMessageAsync(
                        token,
                        request.Title,
                        request.Message
                    );
                }


                return Ok(new
                {
                    success = true,
                    message = $"{deviceTokens.Count} faculty notifications sent successfully."
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