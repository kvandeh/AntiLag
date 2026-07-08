using Rocket.Core.Plugins;
using SDG.Unturned;

namespace AntiLag
{
    public class AntiLagPlugin : RocketPlugin
    {
        protected override void Load()
        {
            Rocket.Core.Logging.Logger.Log($"AntiLag {Assembly.GetName().Version} loaded, monitoring {Provider.clients.Count} players.");
        }

        protected override void Unload()
        {
            Rocket.Core.Logging.Logger.Log("AntiLag unloaded.");
        }
    }
}
