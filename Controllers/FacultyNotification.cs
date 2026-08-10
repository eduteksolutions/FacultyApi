using FacultyApi.model;
using FacultyApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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

                List<string> deviceTokens = new List<string>();

                // Scoped block to ensure the reader closes immediately after fetching tokens
                {
                    SqlCommand cmd = new SqlCommand(
                        @"SELECT DeviceToken
                      FROM HRDStaffMaster
                      WHERE UserID = @UserID
                      AND DeviceToken IS NOT NULL",
                        con);

                    cmd.Parameters.AddWithValue("@UserID", request.UserID);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string? token = reader["DeviceToken"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            deviceTokens.Add(token);
                        }
                    }
                } // <-- Reader is completely disposed here, freeing up the connection!

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
                    INSERT INTO FacultyNotificationLogs (UserID, Title, Message, DeviceToken, Status, ErrorMessage, CreatedAt, IsRead)
                    VALUES (@UserID, @Title, @Message, @DeviceToken, @Status, @ErrorMessage, GETDATE(), 0)";

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

        [HttpPost("markAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                SqlCommand cmd = new SqlCommand(
                    "UPDATE FacultyNotificationLogs SET IsRead = 1 WHERE Id = @Id",
                    con);

                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true, message = "Marked as read." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("unreadCount/{userId}")]
        public async Task<IActionResult> GetUnreadCount(string userId)
        {
            try
            {
                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                SqlCommand cmd = new SqlCommand(
                    @"SELECT COUNT(*) 
                  FROM FacultyNotificationLogs 
                  WHERE UserID = @UserID AND (IsRead = 0 OR IsRead IS NULL)",
                    con);

                cmd.Parameters.AddWithValue("@UserID", userId);
                int count = (int)await cmd.ExecuteScalarAsync();

                return Ok(new { success = true, unreadCount = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("facultyNotificationlogs/{userId}")]
        public async Task<IActionResult> GetFacultyNotificationLogs(string userId)
        {
            try
            {
                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                // Added IsRead to the SELECT statement
                SqlCommand cmd = new SqlCommand(
                    @"SELECT Id, UserID, Title, Message, DeviceToken, Status, ErrorMessage, CreatedAt, IsRead
                  FROM FacultyNotificationLogs
                  WHERE UserID = @UserID
                  ORDER BY CreatedAt DESC",
                    con);

                cmd.Parameters.AddWithValue("@UserID", userId);

                List<object> logs = new List<object>();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new
                    {
                        Id = reader["Id"],
                        UserID = reader["UserID"]?.ToString(),
                        Title = reader["Title"]?.ToString(),
                        Message = reader["Message"]?.ToString(),
                        DeviceToken = reader["DeviceToken"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        ErrorMessage = reader["ErrorMessage"]?.ToString(),
                        CreatedAt = reader["CreatedAt"],
                        IsRead = reader["IsRead"] != DBNull.Value && Convert.ToBoolean(reader["IsRead"])
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = logs
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