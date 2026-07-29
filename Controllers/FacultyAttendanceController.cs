using FacultyApi.Data;
using FacultyApi.model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacultyAttendanceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public FacultyAttendanceController(
    ApplicationDbContext context,
    IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpPost]
        [Route("GetAttendanceStatus")]
        public async Task<IActionResult> GetAttendanceStatus([FromBody] AttendanceStatusRequest request)
        {
            try
            {
                var attendance = await _context.HRDCardAttendances
        .FirstOrDefaultAsync(x =>
            x.empCode == request.FacultyCd &&
            x.UserID == request.UserId &&
            x.aDate == request.S_Date);


                if (attendance == null)
                {
                    return Ok(new
                    {
                        status = true,
                        punchIn = false,
                        punchOut = false,
                        inTime = "--:--",
                        outTime = "--:--",
                        message = "No attendance found"
                    });
                }


                return Ok(new
                {
                    status = true,

                    punchIn = attendance.inTime != null,

                    punchOut = attendance.outTime != null,

                    inTime = attendance.inTime != null
        ? attendance.inTime.Value.ToString("hh:mm tt")
        : "--:--",

                    outTime = attendance.outTime != null
        ? attendance.outTime.Value.ToString("hh:mm tt")
        : "--:--",

                    message = "Attendance status loaded"
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        // POST: api/FacultyAttendance/PunchIn
        [HttpPost("PunchIn")]
        public async Task<IActionResult> PunchIn([FromBody] PunchInRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        Status = false,
                        Message = "Invalid request"
                    });
                }


                // Check already punched in today
                var existingAttendance = await _context.HRDCardAttendances
                    .FirstOrDefaultAsync(x =>
                        x.empCode == model.FacultyCd &&
                        x.UserID == model.UserId &&
                        x.aDate.Date == model.S_Date.Date
                    );


                if (existingAttendance != null)
                {
                    return Ok(new
                    {
                        Code = "500",
                        Status = false,
                        Message = "Attendance already marked for today",
                        TransactionStatus = 0
                    });
                }



                int result = 0;

                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));


                using SqlCommand cmd = new SqlCommand(
                    "InsertHRDCardAttendance",
                    con);


                cmd.CommandType = System.Data.CommandType.StoredProcedure;


                cmd.Parameters.AddWithValue("@aDate", model.S_Date);

                cmd.Parameters.AddWithValue("@empCode", model.FacultyCd);

                cmd.Parameters.AddWithValue("@reason",
                    model.Reason ?? "");

                cmd.Parameters.AddWithValue("@Status",
                    model.Status ?? "P");

                cmd.Parameters.AddWithValue("@facultyname",
                    model.FacultyName ?? "");

                cmd.Parameters.AddWithValue("@latitude",
                    model.Latitude ?? "");

                cmd.Parameters.AddWithValue("@longitude",
                    model.Longitude ?? "");


                cmd.Parameters.AddWithValue("@inTime",
                    string.IsNullOrEmpty(model.EInTime)
                    ? DBNull.Value
                    : model.EInTime);


                cmd.Parameters.AddWithValue("@outTime",
                    DBNull.Value);


                cmd.Parameters.AddWithValue("@UserID",
                    model.UserId);



                SqlParameter transStatus = new SqlParameter(
                    "@TransStatus",
                    System.Data.SqlDbType.Int)
                {
                    Direction = System.Data.ParameterDirection.Output
                };


                cmd.Parameters.Add(transStatus);


                await con.OpenAsync();


                await cmd.ExecuteNonQueryAsync();


                result = Convert.ToInt32(transStatus.Value);



                return Ok(new
                {
                    code = result == 1 ? "200" : "500",

                    status = result == 1,

                    message = result switch
                    {
                        1 => "Punch In Successfully",
                        2 => "Punch Out Already Done",
                        3 => "Punch Out Successfully",
                        _ => "Punch Failed"
                    },

                    transactionStatus = result
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }


        // POST: api/FacultyAttendance/PunchOut
        [HttpPost("PunchOut")]
        public async Task<IActionResult> PunchOut([FromBody] PunchOutRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request"
                    });
                }


                // Check today's attendance
                var attendance = await _context.HRDCardAttendances
                    .FirstOrDefaultAsync(x =>
                        x.empCode == model.FacultyCd &&
                        x.UserID == model.UserId &&
                        x.aDate.Date == model.S_Date.Date
                    );


                // No Punch In found
                if (attendance == null)
                {
                    return Ok(new
                    {
                        code = "500",
                        status = false,
                        message = "Please Punch In first",
                        transactionStatus = 0
                    });
                }


                // Already Punch Out
                if (attendance.outTime != null)
                {
                    return Ok(new
                    {
                        code = "500",
                        status = false,
                        message = "Punch Out already completed",
                        transactionStatus = 0
                    });
                }



                int result = 0;


                using SqlConnection con = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));


                using SqlCommand cmd = new SqlCommand(
                    "UpdateHRDCardAttendanceOutTime",
                    con);


                cmd.CommandType = System.Data.CommandType.StoredProcedure;


                cmd.Parameters.AddWithValue("@aDate", model.S_Date);

                cmd.Parameters.AddWithValue("@empCode", model.FacultyCd);

                cmd.Parameters.AddWithValue("@UserID", model.UserId);

                cmd.Parameters.AddWithValue("@outTime", model.EOutTime);



                await con.OpenAsync();


                result = Convert.ToInt32(await cmd.ExecuteScalarAsync());



                return Ok(new
                {
                    code = result == 1 ? "200" : "500",

                    status = result == 1,

                    message = result == 1
                        ? "Punch Out Updated Successfully"
                        : "Punch Out Failed",

                    transactionStatus = result
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

    }
}