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

                cmd.Parameters.AddWithValue("@reason", model.Reason ?? "");

                cmd.Parameters.AddWithValue("@Status", model.Status ?? "");

                cmd.Parameters.AddWithValue("@facultyname", model.FacultyName ?? "");

                cmd.Parameters.AddWithValue("@latitude", model.Latitude ?? "");

                cmd.Parameters.AddWithValue("@longitude", model.Longitude ?? "");

                cmd.Parameters.AddWithValue("@inTime",
                    string.IsNullOrEmpty(model.EInTime)
                    ? DBNull.Value
                    : model.EInTime);

                cmd.Parameters.AddWithValue("@outTime", DBNull.Value);

                cmd.Parameters.AddWithValue("@UserID", model.UserId);



                // OUTPUT PARAMETER
                SqlParameter transStatus = new SqlParameter(
                    "@TransStatus",
                    System.Data.SqlDbType.Int)
                {
                    Direction = System.Data.ParameterDirection.Output
                };

                cmd.Parameters.Add(transStatus);



                con.Open();

                // Execute SP
                cmd.ExecuteNonQuery();


                // Get output value
                result = Convert.ToInt32(transStatus.Value);



                return Ok(new
                {
                    Code = result == 1 ? "200" : "500",

                    Status = result == 1,

                    Message = result switch
                    {
                        1 => "Punch In Successfully",
                        2 => "Punch Out Already Done",
                        3 => "Punch Out Successfully",
                        _ => "Punch Failed"
                    },

                    TransactionStatus = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = false,
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
                        ? "Punch Out Updated Successfully"
                        : "Punch Out Failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }
    }
}