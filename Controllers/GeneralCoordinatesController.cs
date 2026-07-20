using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeneralCoordinatesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public GeneralCoordinatesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // GET: api/GeneralCoordinates/GetByUserID?userid=1

        [HttpGet("GetByUserID")]
        public IActionResult GetByUserID(int userid)
        {
            List<object> list = new();

            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            SqlCommand cmd = new SqlCommand(@"
        SELECT 
            Code,
            UserID,
            Latitude,
            Longitude
        FROM edu.GeneralCoordinatesMaster
        WHERE UserID = @UserID", con);


            cmd.Parameters.AddWithValue("@UserID", userid);


            con.Open();

            SqlDataReader dr = cmd.ExecuteReader();


            while (dr.Read())
            {
                list.Add(new
                {
                    Code = dr["Code"],
                    UserID = dr["UserID"],
                    Latitude = dr["Latitude"],
                    Longitude = dr["Longitude"]
                });
            }


            return Ok(list);
        }
        // API methods here
    }
}
