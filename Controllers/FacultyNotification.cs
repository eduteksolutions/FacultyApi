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

                int successCount = 0;

                foreach (var token in deviceTokens)
                {
                    string status = "Success";
                    string? errorMessage = null;

                    try
                    {
                        await _notificationService.SendMessageAsync(
                            token,
                            request.Title,
                            request.Message
                        );
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        status = "Failed";
                        errorMessage = ex.Message;
                    }

                    string insertLogQuery = @"
                INSERT INTO TableFacultyNotificationLogs (UserID, Title, Message, DeviceToken, Status, ErrorMessage, CreatedAt)
                VALUES (@UserID, @Title, @Message, @DeviceToken, @Status, @ErrorMessage, GETDATE())";

                    using SqlCommand logCmd = new SqlCommand(insertLogQuery, con);
                    logCmd.Parameters.AddWithValue("@UserID", request.UserID);
                    logCmd.Parameters.AddWithValue("@Title", request.Title ?? (object)DBNull.Value);
                    logCmd.Parameters.AddWithValue("@Message", request.Message ?? (object)DBNull.Value);
                    logCmd.Parameters.AddWithValue("@DeviceToken", token);
                    logCmd.Parameters.AddWithValue("@Status", status);
                    logCmd.Parameters.AddWithValue("@ErrorMessage", string.IsNullOrEmpty(errorMessage) ? (object)DBNull.Value : errorMessage);

                    await logCmd.ExecuteNonQueryAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = $"{successCount} of {deviceTokens.Count} faculty notifications processed successfully."
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