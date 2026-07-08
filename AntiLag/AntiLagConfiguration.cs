using Rocket.API;

namespace AntiLag
{
    public class AntiLagConfiguration : IRocketPluginConfiguration
    {
        public float GapThresholdSeconds;
        public float SuspicionWindowSeconds;
        public float DamageMultiplier;
        public bool NotifyAdmins;
        public string DiscordWebhookUrl;

        public void LoadDefaults()
        {
            GapThresholdSeconds = 0.5f;
            SuspicionWindowSeconds = 3f;
            DamageMultiplier = 1f;
            NotifyAdmins = true;
            DiscordWebhookUrl = "";
        }
    }
}
