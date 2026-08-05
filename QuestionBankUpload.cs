using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace FacultyApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionBankUploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public QuestionBankUploadController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("GetQuestions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQuestions(
            [FromQuery] int classCd,
            [FromQuery] int subjectCd,
            [FromQuery] string? board,
            [FromQuery] string? chapter,
            [FromQuery] int? userId)
        {
            try
            {
                int loginUserId = userId ?? 1; // Default UserId = 1

                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await con.OpenAsync();

                string query = @"
                    SELECT
                        qId,
                        TestID,
                        classCd,
                        subId,
                        Board,
                        Chapter,
                        Question,
                        anyRemark,
                        qOpt1,
                        qOpt2,
                        qOpt3,
                        qOpt4,
                        qAns
                    FROM questionBank
                    WHERE classCd = @ClassCd
                      AND subId = @SubId
                      AND UserID = @UserID";

                if (!string.IsNullOrEmpty(board))
                {
                    query += " AND Board = @Board";
                }

                if (!string.IsNullOrEmpty(chapter))
                {
                    query += " AND Chapter = @Chapter";
                }

                query += " ORDER BY qId";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ClassCd", classCd);
                cmd.Parameters.AddWithValue("@SubId", subjectCd);
                cmd.Parameters.AddWithValue("@UserID", loginUserId);

                if (!string.IsNullOrEmpty(board))
                {
                    cmd.Parameters.AddWithValue("@Board", board);
                }

                if (!string.IsNullOrEmpty(chapter))
                {
                    cmd.Parameters.AddWithValue("@Chapter", chapter);
                }

                List<QuestionItemDto> questions = new();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    questions.Add(new QuestionItemDto
                    {
                        FileId = Convert.ToInt32(reader["qId"]),
                        Board = reader["Board"]?.ToString() ?? "",
                        Chapter = reader["Chapter"]?.ToString() ?? "",
                        Topic = reader["anyRemark"]?.ToString() ?? "",
                        Question = reader["Question"]?.ToString() ?? "",
                        Options = new List<string>
                        {
                            reader["qOpt1"]?.ToString() ?? "",
                            reader["qOpt2"]?.ToString() ?? "",
                            reader["qOpt3"]?.ToString() ?? "",
                            reader["qOpt4"]?.ToString() ?? ""
                        },
                        CorrectAnswer = Convert.ToInt32(reader["qAns"])
                    });
                }

                return Ok(new
                {
                    Status = true,
                    Message = "Questions fetched successfully.",
                    Count = questions.Count,
                    Data = questions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = false,
                    Message = "Error: " + ex.Message
                });
            }
        }

        public class QuestionItemDto
        {
            public int FileId { get; set; }
            public string Board { get; set; } = "";
            public string Chapter { get; set; } = "";
            public string Topic { get; set; } = "";
            public string Question { get; set; } = "";
            public List<string> Options { get; set; } = new();
            public int CorrectAnswer { get; set; }
        }

        [HttpPost("UploadAndSaveJsonQuestions")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadAndSaveJsonQuestions(
            [FromQuery] int testId,
            [FromQuery] int classCd,
            [FromQuery] int subjectCd,
            [FromQuery] string? board,
            [FromQuery] string? chapter,
            [FromQuery] int userId,
            IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Status = false, Message = "Please select a valid JSON file to upload." });
                }

                List<QuestionItemDto> jsonQuestions;

                using (var stream = file.OpenReadStream())
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    jsonQuestions = await JsonSerializer.DeserializeAsync<List<QuestionItemDto>>(stream, options);
                }

                if (jsonQuestions == null || jsonQuestions.Count == 0)
                {
                    return BadRequest(new { Status = false, Message = "No questions found in the uploaded JSON file." });
                }

                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await con.OpenAsync();
                int insertedCount = 0;

                foreach (var q in jsonQuestions)
                {
                    // Fetch next unique qId per User and Class
                    using SqlCommand idCmd = new SqlCommand(
                        "SELECT ISNULL(MAX(qId), 0) + 1 FROM questionBank WHERE UserID = @UserID AND classCd = @ClassCd",
                        con);

                    idCmd.Parameters.AddWithValue("@UserID", userId);
                    idCmd.Parameters.AddWithValue("@ClassCd", classCd);

                    int questionId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());

                    using SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO questionBank
                        (
                            TestID, qId, subId, Board, Chapter, qLevel, qMarks, qType, classCd,
                            Question, qMode, anyRemark,
                            qOpt1, qOpt2, qOpt3, qOpt4,
                            qAns, LoginName, LuserDt, UserID, testSequenceNum
                        )
                        VALUES
                        (
                            @TestID, @QId, @SubId, @Board, @Chapter, 1, 1, 'S', @ClassCd,
                            @Question, 'O', @Remark,
                            @Opt1, @Opt2, @Opt3, @Opt4,
                            @Ans, 'JsonUpload', GETDATE(), @UserID, @Seq
                        )", con);

                    cmd.Parameters.AddWithValue("@TestID", testId);
                    cmd.Parameters.AddWithValue("@QId", questionId);
                    cmd.Parameters.AddWithValue("@SubId", subjectCd);
                    cmd.Parameters.AddWithValue("@Board", string.IsNullOrEmpty(board) ? (object)DBNull.Value : board);
                    cmd.Parameters.AddWithValue("@Chapter", string.IsNullOrEmpty(chapter) ? (object)DBNull.Value : chapter);
                    cmd.Parameters.AddWithValue("@ClassCd", classCd);
                    cmd.Parameters.AddWithValue("@Question", q.Question);
                    cmd.Parameters.AddWithValue("@Remark", q.Topic ?? "");
                    cmd.Parameters.AddWithValue("@Opt1", q.Options.Count > 0 ? q.Options[0] : "");
                    cmd.Parameters.AddWithValue("@Opt2", q.Options.Count > 1 ? q.Options[1] : "");
                    cmd.Parameters.AddWithValue("@Opt3", q.Options.Count > 2 ? q.Options[2] : "");
                    cmd.Parameters.AddWithValue("@Opt4", q.Options.Count > 3 ? q.Options[3] : "");
                    cmd.Parameters.AddWithValue("@Ans", q.CorrectAnswer);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Seq", questionId);

                    insertedCount += await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new
                {
                    Status = true,
                    Message = $"{insertedCount} questions successfully imported and saved with Board and Chapter information."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = false,
                    Message = "Error: " + ex.Message
                });
            }
        }
    }
}