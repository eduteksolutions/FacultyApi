using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FacultyApi.Data;
//this For Notification Servcies
using FacultyApi.model;
using FacultyApi.Services;

namespace FacultyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultyNotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public FacultyNotificationController(
            ApplicationDbContext context,
            NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }


        // 📢 Send Notification To Faculty
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification(
            [FromBody] FacultyNotificationRequest request)
        {
            try
            {
                var faculty = await _context.HRDStaffMaster
                    .FirstOrDefaultAsync(x => x.UserID == request.UserID);


                if (faculty == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Faculty not found"
                    });
                }


                if (string.IsNullOrEmpty(faculty.DeviceToken))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Faculty device token not available"
                    });
                }


                await _notificationService.SendMessageAsync(
                    faculty.DeviceToken,
                    request.Title,
                    request.Message
                );


                return Ok(new
                {
                    success = true,
                    message = "Notification sent successfully"
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