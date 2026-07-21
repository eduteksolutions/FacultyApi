
using FacultyApi.Internal;
using FacultyApi.model;
using Microsoft.Data.SqlClient;

namespace FacultyApi.Services

{


    public class GeneralCoordinatesService : IGeneralCoordinatesService
    {
        private readonly IConfiguration _configuration;

        public GeneralCoordinatesService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ApiResponse GetByUserID(int userid)
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
        WHERE UserID=@UserID", con);

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

            if (list.Count > 0)
            {
                return new ApiResponse
                {
                    Status = true,
                    Message = "Record found successfully.",
                    Data = list
                };
            }
            else
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = "Record not found.",
                    Data = new List<object>()
                };
            }
        }


    }
}
    

