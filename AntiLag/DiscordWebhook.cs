using System.Net.Http;
using System.Text;
using Logger = Rocket.Core.Logging.Logger;

namespace AntiLag
{
    public static class DiscordWebhook
    {
        private static readonly HttpClient http = new HttpClient();

        public static async void Send(string url, string report, byte[] screenshotJpg)
        {
            string image = screenshotJpg == null ? "" : ",\"image\":{\"url\":\"attachment://spy.jpg\"}";
            string payload = "{\"embeds\":[{\"title\":\"AntiLag flag\",\"description\":" +
                ToJsonString(report) + ",\"color\":15158332" + image + "}]}";

            var form = new MultipartFormDataContent
            {
                { new StringContent(payload, Encoding.UTF8, "application/json"), "payload_json" }
            };
            if (screenshotJpg != null)
            {
                form.Add(new ByteArrayContent(screenshotJpg), "files[0]", "spy.jpg");
            }

            try
            {
                HttpResponseMessage response = await http.PostAsync(url, form);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning($"Discord webhook returned {(int)response.StatusCode}");
                }
            }
            catch (HttpRequestException exception)
            {
                Logger.LogWarning("Discord webhook failed: " + exception.Message);
            }
        }

        private static string ToJsonString(string value) =>
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
    }
}
