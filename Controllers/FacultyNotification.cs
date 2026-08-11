using FacultyApi.model;
using FacultyApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class FacultyNotificationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;
        private const string ApiVersion = "v1";

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
                version = $"FacultyNotificationController-{ApiVersion}",
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

                var staffRecs = new List<(string DeviceToken, int StaffId, string Code)>();

                {
                    SqlCommand cmd = new SqlCommand(
                        @"SELECT DeviceToken, id, code
                        FROM HRDStaffMaster
                        WHERE (@UserID IS NULL OR UserID = @UserID)
                          AND (@Code IS NULL OR code = @Code)
                          AND DeviceToken IS NOT NULL",
                        con);

                    cmd.Parameters.AddWithValue("@UserID", string.IsNullOrEmpty(request.UserID) ? (object)DBNull.Value : request.UserID);
                    cmd.Parameters.AddWithValue("@Code", string.IsNullOrEmpty(request.Code) ? (object)DBNull.Value : request.Code);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string? token = reader["DeviceToken"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            int staffId = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;
                            string code = reader["code"]?.ToString() ?? string.Empty;
                            staffRecs.Add((token, staffId, code));
                        }
                    }
                }

                if (staffRecs.Count == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No faculty device tokens found for the provided criteria."
                    });
                }

                int successCount = 0;

                foreach (var staff in staffRecs)
                {
                    string status = "Success";
                    string? errorMessage = null;

                    try
                    {
                        await _notificationService.SendMessageAsync(
                            staff.DeviceToken,
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
                        INSERT INTO FacultyNotificationLogs 
                        (UserID, id,  Title, Message, DeviceToken, Status, ErrorMessage, Version, CreatedAt, IsRead)
                        VALUES 
                        (@UserID, @id,  @Title, @Message, @DeviceToken, @Status, @ErrorMessage, @Version, GETDATE(), 0)";

                    using SqlCommand logCmd = new SqlCommand(insertLogQuery, con);
                    logCmd.Parameters.AddWithValue("@UserID", string.IsNullOrEmpty(request.UserID) ? (object)DBNull.Value : request.UserID);
                    logCmd.Parameters.AddWithValue("@Id", staff.StaffId > 0 ? staff.StaffId : (object)DBNull.Value);
                    logCmd.Parameters.AddWithValue("@Title", request.Title ?? (object)DBNull.Value);
                    logCmd.Parameters.AddWithValue("@Message", request.Message ?? (object)DBNull.Value);
                    logCmd.Parameters.AddWithValue("@DeviceToken", staff.DeviceToken);
                    logCmd.Parameters.AddWithValue("@Status", status);
                    logCmd.Parameters.AddWithValue("@ErrorMessage", string.IsNullOrEmpty(errorMessage) ? (object)DBNull.Value : errorMessage);
                    logCmd.Parameters.AddWithValue("@Version", ApiVersion);

                    await logCmd.ExecuteNonQueryAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = $"{successCount} of {staffRecs.Count} faculty notifications processed successfully."
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
        public async Task<IActionResult> MarkAsRead(int id,int userid)
        {
            try
            {
                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                SqlCommand cmd = new SqlCommand(
                    "UPDATE FacultyNotificationLogs SET IsRead = 1 WHERE Id = @Id  and userid=@userid",
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

        [HttpGet("unreadCount/{schoolid}/{id}")]
        public async Task<IActionResult> GetUnreadCount(string schoolid, string id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                int? parsedId = int.TryParse(id, out int idVal) ? idVal : (int?)null;

                SqlCommand cmd = new SqlCommand(
                    @"SELECT COUNT(*) 
            FROM FacultyNotificationLogs 
            WHERE UserID = @SchoolId 
              AND (id = @Id OR (@ParsedId IS NOT NULL AND id = @ParsedId)) 
              AND (IsRead = 0 OR IsRead IS NULL)",
                    con);

                cmd.Parameters.AddWithValue("@SchoolId", schoolid);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@ParsedId", parsedId ?? (object)DBNull.Value);

                int count = (int)await cmd.ExecuteScalarAsync();

                return Ok(new { success = true, unreadCount = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("facultyNotificationlogs/{schoolid}/{id}")]
        public async Task<IActionResult> GetFacultyNotificationLogs(string schoolid, string id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                await con.OpenAsync();

                int? parsedId = int.TryParse(id, out int idVal) ? idVal : (int?)null;

                SqlCommand cmd = new SqlCommand(
                    @"SELECT Id, UserID, StaffId, StaffCode, Title, Message, DeviceToken, Status, ErrorMessage, Version, CreatedAt, IsRead
            FROM FacultyNotificationLogs
            WHERE UserID = @SchoolId 
              AND (id = @Id OR (@ParsedId IS NOT NULL AND id = @ParsedId))
            ORDER BY CreatedAt DESC",
                    con);

                cmd.Parameters.AddWithValue("@SchoolId", schoolid);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@ParsedId", parsedId ?? (object)DBNull.Value);

                List<object> logs = new List<object>();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new
                    {
                        Id = reader["Id"],
                        UserID = reader["UserID"]?.ToString(),
                        StaffId = reader["StaffId"] != DBNull.Value ? reader["StaffId"] : null,
                        StaffCode = reader["StaffCode"]?.ToString(),
                        Title = reader["Title"]?.ToString(),
                        Message = reader["Message"]?.ToString(),
                        DeviceToken = reader["DeviceToken"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        ErrorMessage = reader["ErrorMessage"]?.ToString(),
                        Version = reader["Version"]?.ToString(),
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