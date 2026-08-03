using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Text;

namespace FacultyApi.Services
{
    public interface INotificationService
    {
        Task<string> GetAccessTokenAsync();
        Task<string> SendMessageAsync(string deviceToken, string title, string body);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string FirebaseProjectId = "schoolapp-3410c";

        // The live URL containing your service account credentials file
        private const string CredentialsUrl = "https://eduteksolutions.in/WebSite/schoolapp-3410c-firebase-adminsdk-fbsvc-2b89c982c3.json";

        public NotificationService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ==========================================================
        // DYNAMICALLY FETCH CREDENTIALS AND GENERATE OAUTH 2.0 TOKEN
        // ==========================================================
        public async Task<string> GetAccessTokenAsync()
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                // Pulls the JSON directly from your website endpoint
                string jsonContent = await client.GetStringAsync(CredentialsUrl);

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
                {
                    var credential = GoogleCredential.FromStream(stream)
                        .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

                    return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                }
            }
        }

        // ==========================================================
        // SEND PUSH NOTIFICATION (FCM v1 HTTP)
        // ==========================================================
        public async Task<string> SendMessageAsync(string deviceToken, string title, string body)
        {
            string accessToken = await GetAccessTokenAsync();
            string uniqueId = Guid.NewGuid().ToString();

            using (var client = _httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var payload = new
                {
                    message = new
                    {
                        token = deviceToken,
                        notification = new { title, body },
                        android = new
                        {
                            notification = new
                            {
                                sound = "default",
                                channel_id = "high_importance_channel",
                                tag = uniqueId
                            }
                        },
                        data = new
                        {
                            click_action = "FLUTTER_NOTIFICATION_CLICK",
                            messageId = Guid.NewGuid().ToString()
                        }
                    }
                };

                var jsonMessage = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                string url = $"https://fcm.googleapis.com/v1/projects/{FirebaseProjectId}/messages:send";

                var response = await client.PostAsync(url, content);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
