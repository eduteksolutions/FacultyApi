using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FacultyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentAppClassWiseUsageController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StudentAppClassWiseUsageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Students")]
        public async Task<IActionResult> GetStudents(
            int userID,
            int classCd)
        {
            var students = new List<object>();

            string connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(
                "sp_GetStudentAppUsageStatus", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                cmd.Parameters.Add("@ClassCd", SqlDbType.Int).Value = classCd;

                await con.OpenAsync();

                using (SqlDataReader reader =
                       await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        students.Add(new
                        {
                            Name = reader["Name"]?.ToString(),
                            AdmCd = Convert.ToInt32(reader["AdmCd"]),
                            RollNo = reader["RollNo"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader["RollNo"]),
                            Status = reader["Status"]?.ToString()
                        });
                    }
                }
            }

            return Ok(new
            {
                success = true,
                data = students
            });
        }
    }
}