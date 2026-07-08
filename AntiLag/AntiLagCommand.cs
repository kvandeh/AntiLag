using Rocket.API;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

namespace AntiLag
{
    public class AntiLagCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "antilag";
        public string Help => "List online players flagged for lagswitching this session";
        public string Syntax => "";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "antilag" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            List<KeyValuePair<CSteamID, PlayerLagState>> flagged = AntiLagPlugin.Instance.States
                .Where(pair => pair.Value.Strikes > 0)
                .OrderByDescending(pair => pair.Value.Strikes)
                .ToList();

            if (flagged.Count == 0)
            {
                UnturnedChat.Say(caller, "AntiLag: no flags this session.");
                return;
            }
            foreach (KeyValuePair<CSteamID, PlayerLagState> pair in flagged)
            {
                string name = PlayerTool.getSteamPlayer(pair.Key)?.playerID.characterName ?? pair.Key.ToString();
                UnturnedChat.Say(caller, $"AntiLag: {name} — {pair.Value.Strikes} strikes");
            }
        }
    }
}
