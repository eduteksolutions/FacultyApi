
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

        private SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));
        }

        // Get All
        public ApiResponse GetAll()
        {
            List<object> list = new();

            using SqlConnection con = GetConnection();

            SqlCommand cmd = new SqlCommand(@"
                SELECT Code, UserID, Latitude, Longitude
                FROM edu.GeneralCoordinatesMaster", con);

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

            return new ApiResponse
            {
                Status = true,
                Message = "Records fetched successfully.",
                Data = list
            };
        }

        // Get By UserIDa
        public ApiResponse GetByUserID(int userId)
        {
            List<object> list = new();

            using SqlConnection con = GetConnection();

            SqlCommand cmd = new SqlCommand(@"
                SELECT Code, UserID, Latitude, Longitude
                FROM edu.GeneralCoordinatesMaster
                WHERE UserID=@UserID", con);

            cmd.Parameters.AddWithValue("@UserID", userId);

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

            return new ApiResponse
            {
                Status = list.Count > 0,
                Message = list.Count > 0 ? "Record found." : "Record not found.",
                Data = list
            };
        }

        // Insert
        public ApiResponse Insert(GeneralCoordinates model)
        {
            using SqlConnection con = GetConnection();

            con.Open();

            // Check record count
            SqlCommand countCmd = new SqlCommand(@"
        SELECT COUNT(*) 
        FROM edu.GeneralCoordinatesMaster
        WHERE UserID = @UserID", con);

            countCmd.Parameters.AddWithValue("@UserID", model.UserID);

            int count = Convert.ToInt32(countCmd.ExecuteScalar());

            if (count > 0)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = "Record already exists.",
                    Data = new List<object>()
                };
            }

            // Insert record
            SqlCommand cmd = new SqlCommand(@"
        INSERT INTO edu.GeneralCoordinatesMaster
        (UserID, Latitude, Longitude)
        VALUES
        (@UserID, @Latitude, @Longitude)", con);

            cmd.Parameters.AddWithValue("@UserID", model.UserID);
            cmd.Parameters.AddWithValue("@Latitude", model.Latitude);
            cmd.Parameters.AddWithValue("@Longitude", model.Longitude);

            int result = cmd.ExecuteNonQuery();

            return new ApiResponse
            {
                Status = result > 0,
                Message = result > 0
                    ? "Record inserted successfully."
                    : "Insert failed."
            };
        }
        // Update
        public ApiResponse Update(GeneralCoordinates model)
        {
            using SqlConnection con = GetConnection();

            SqlCommand cmd = new SqlCommand(@"
                UPDATE edu.GeneralCoordinatesMaster
                SET Latitude=@Latitude,
                    Longitude=@Longitude
                WHERE Code=@Code", con);

            cmd.Parameters.AddWithValue("@Code", model.Code);
            cmd.Parameters.AddWithValue("@Latitude", model.Latitude);
            cmd.Parameters.AddWithValue("@Longitude", model.Longitude);

            con.Open();

            int result = cmd.ExecuteNonQuery();

            return new ApiResponse
            {
                Status = result > 0,
                Message = result > 0 ? "Record updated successfully." : "Update failed."
            };
        }

        // Delete
        public ApiResponse Delete(int code)
        {
            using SqlConnection con = GetConnection();

            SqlCommand cmd = new SqlCommand(@"
                DELETE FROM edu.GeneralCoordinatesMaster
                WHERE Code=@Code", con);

            cmd.Parameters.AddWithValue("@Code", code);

            con.Open();

            int result = cmd.ExecuteNonQuery();

            return new ApiResponse
            {
                Status = result > 0,
                Message = result > 0 ? "Record deleted successfully." : "Delete failed."
            };
        }
    }
}