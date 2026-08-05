using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class GeminiAiTestMasterController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiAiTestMasterController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public class AiQuestionItem
    {
        public string Question { get; set; } = "";
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = "";
        public string Explanation { get; set; } = "";
    }

    public class AiQuizContainer
    {
        public string Topic { get; set; } = "";
        public List<AiQuestionItem> Questions { get; set; } = new();
    }

    [HttpPost("GenerateAndSaveAiTest")]
    public async Task<IActionResult> GenerateAndSaveAiTest(
        [FromQuery] int testId,
        [FromQuery] int classCd,
        [FromQuery] int subjectCd,
        [FromQuery] int userId,
        [FromQuery] string topic,
        [FromQuery] int count = 5)
    {
        try
        {
            string apiKey = _configuration["GeminiApiKey"]
                ?? _configuration["GEMINI_API_KEY"]
                ?? Environment.GetEnvironmentVariable("GeminiApiKey")
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? string.Empty;

            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Gemini API Key is missing. Please set it in appsettings.json or via the GEMINI_API_KEY environment variable."
                });
            }

            string promptText = $"Generate {count} multiple-choice test questions about {topic}. Provide 4 options and clearly state the correct answer. Format the output strictly as JSON matching a structure with a 'questions' array containing objects with Question, Options (array of 4 strings), CorrectAnswer, and Explanation.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.3
                }
            };

            var httpClient = _httpClientFactory.CreateClient();

            // Updated to use a valid active production model endpoint
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
            var httpMessage = new HttpRequestMessage(HttpMethod.Post, url);
            httpMessage.Headers.Add("x-goog-api-key", apiKey);

            string jsonBody = JsonSerializer.Serialize(requestBody);
            httpMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var apiResponse = await httpClient.SendAsync(httpMessage);
            string responseString = await apiResponse.Content.ReadAsStringAsync();

            if (!apiResponse.IsSuccessStatusCode)
            {
                return Ok(new
                {
                    Status = false,
                    Message = $"Gemini API Error: {responseString}"
                });
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            string jsonText = "{}";

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    jsonText = parts[0].GetProperty("text").GetString() ?? "{}";
                }
            }

            // Clean markdown code ticks if returned by the model
            jsonText = jsonText.Trim();
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            else if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }
            jsonText = jsonText.Trim();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<AiQuestionItem> questionList = new();

            try
            {
                var quiz = JsonSerializer.Deserialize<AiQuizContainer>(jsonText, jsonOptions);
                if (quiz?.Questions != null && quiz.Questions.Count > 0)
                {
                    questionList = quiz.Questions;
                }
                else
                {
                    questionList = JsonSerializer.Deserialize<List<AiQuestionItem>>(jsonText, jsonOptions) ?? new();
                }
            }
            catch
            {
                questionList = new();
            }

            if (questionList.Count == 0)
            {
                return Ok(new
                {
                    Status = false,
                    Message = "AI did not generate any valid questions."
                });
            }

            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            await con.OpenAsync();
            int inserted = 0;

            foreach (var q in questionList)
            {
                using SqlCommand idCmd = new SqlCommand(
                    "SELECT ISNULL(MAX(qId), 0) + 1 FROM questionBank WHERE UserID = @UserID AND classCd = @ClassCd",
                    con);

                idCmd.Parameters.AddWithValue("@UserID", userId);
                idCmd.Parameters.AddWithValue("@ClassCd", classCd);

                int questionId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());

                int answerIndex = 1;
                for (int i = 0; i < q.Options.Count; i++)
                {
                    if (q.Options[i].Trim().Equals(q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        answerIndex = i + 1;
                        break;
                    }
                }

                using SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO questionBank
                    (
                        TestID, qId, subId, qLevel, qMarks, qType, classCd,
                        Question, qMode, anyRemark,
                        qOpt1, qOpt2, qOpt3, qOpt4,
                        qAns, LoginName, LuserDt, UserID, testSequenceNum
                    )
                    VALUES
                    (
                        @TestID, @QId, @SubId, 1, 1, 'S', @ClassCd,
                        @Question, 'O', @Remark,
                        @Opt1, @Opt2, @Opt3, @Opt4,
                        @Ans, 'GeminiAI', GETDATE(), @UserID, @Seq
                    )", con);

                cmd.Parameters.AddWithValue("@TestID", testId);
                cmd.Parameters.AddWithValue("@QId", questionId);
                cmd.Parameters.AddWithValue("@SubId", subjectCd);
                cmd.Parameters.AddWithValue("@ClassCd", classCd);
                cmd.Parameters.AddWithValue("@Question", q.Question);
                cmd.Parameters.AddWithValue("@Remark", q.Explanation ?? "");
                cmd.Parameters.AddWithValue("@Opt1", q.Options.Count > 0 ? q.Options[0] : "");
                cmd.Parameters.AddWithValue("@Opt2", q.Options.Count > 1 ? q.Options[1] : "");
                cmd.Parameters.AddWithValue("@Opt3", q.Options.Count > 2 ? q.Options[2] : "");
                cmd.Parameters.AddWithValue("@Opt4", q.Options.Count > 3 ? q.Options[3] : "");
                cmd.Parameters.AddWithValue("@Ans", answerIndex);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@Seq", questionId);

                inserted += await cmd.ExecuteNonQueryAsync();
            }

            return Ok(new
            {
                Status = true,
                Message = $"{inserted} questions generated and saved successfully."
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                Status = false,
                Message = "Error: " + ex.Message
            });
        }
    }
}