using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace AntiLag
{
    public class AntiLagPlugin : RocketPlugin<AntiLagConfiguration>
    {
        public static AntiLagPlugin Instance { get; private set; }

        public readonly Dictionary<CSteamID, PlayerLagState> States = new Dictionary<CSteamID, PlayerLagState>();

        private const float SnapshotIntervalSeconds = 0.5f;
        private const float NotifyCooldownSeconds = 10f;

        private float lastSnapshotTime;

        protected override void Load()
        {
            Instance = this;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            PlayerInput.onPluginKeyTick += OnInputTick;
            DamageTool.damagePlayerRequested += OnDamageRequested;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            Logger.Log($"AntiLag loaded. Gap threshold {Configuration.Instance.GapThresholdSeconds}s, " +
                $"suspicion window {Configuration.Instance.SuspicionWindowSeconds}s, " +
                $"damage multiplier {Configuration.Instance.DamageMultiplier}.");
        }

        protected override void Unload()
        {
            PlayerInput.onPluginKeyTick -= OnInputTick;
            DamageTool.damagePlayerRequested -= OnDamageRequested;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            States.Clear();
            Instance = null;
        }

        private void Update()
        {
            float now = Time.realtimeSinceStartup;
            if (now - lastSnapshotTime < SnapshotIntervalSeconds)
            {
                return;
            }
            lastSnapshotTime = now;
            foreach (SteamPlayer client in Provider.clients)
            {
                GetState(client.playerID.steamID).RecordPosition(now, client.player.transform.position);
            }
        }

        private void OnInputTick(Player player, uint simulation, byte key, bool state)
        {
            if (key != 0)
            {
                return;
            }
            PlayerLagState lagState = GetState(player.channel.owner.playerID.steamID);
            float now = Time.realtimeSinceStartup;
            if (lagState.LastInputTime > 0f)
            {
                float gap = now - lagState.LastInputTime;
                if (gap >= Configuration.Instance.GapThresholdSeconds)
                {
                    lagState.LastGapEnd = now;
                    lagState.LastGapDuration = gap;
                }
            }
            lagState.LastInputTime = now;
        }

        private void OnDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            CSteamID victimId = parameters.player.channel.owner.playerID.steamID;
            if (parameters.killer == victimId)
            {
                return;
            }
            SteamPlayer attacker = PlayerTool.getSteamPlayer(parameters.killer);
            if (attacker == null || !States.TryGetValue(attacker.playerID.steamID, out PlayerLagState state))
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            bool underGamePenalty = attacker.player.input.IsUnderFakeLagPenalty;
            float secondsSinceGap = now - state.LastGapEnd;
            bool recentGap = secondsSinceGap <= Configuration.Instance.SuspicionWindowSeconds;
            if (!underGamePenalty && !recentGap)
            {
                return;
            }

            state.Strikes++;
            parameters.times *= Configuration.Instance.DamageMultiplier;

            if (now - state.LastNotifyTime < NotifyCooldownSeconds)
            {
                return;
            }
            state.LastNotifyTime = now;
            Flag(attacker, BuildReport(attacker, parameters.player, state, secondsSinceGap, underGamePenalty));
        }

        private string BuildReport(SteamPlayer attacker, Player victim, PlayerLagState state,
            float secondsSinceGap, bool underGamePenalty)
        {
            string report = $"{attacker.playerID.characterName} ({attacker.playerID.steamID}) " +
                $"hit {victim.channel.owner.playerID.characterName}";
            if (state.HasRecordedGap)
            {
                report += $" {secondsSinceGap:F1}s after a {state.LastGapDuration:F1}s packet gap; " +
                    $"victim moved {VictimMetersMovedDuringGap(victim, state):F1}m during the gap";
            }
            if (underGamePenalty)
            {
                report += " [game fake-lag penalty active]";
            }
            return report + $". Strikes: {state.Strikes}";
        }

        private float VictimMetersMovedDuringGap(Player victim, PlayerLagState attackerState)
        {
            CSteamID victimId = victim.channel.owner.playerID.steamID;
            if (!States.TryGetValue(victimId, out PlayerLagState victimState))
            {
                return 0f;
            }
            float gapStart = attackerState.LastGapEnd - attackerState.LastGapDuration;
            Vector3? before = victimState.PositionAt(gapStart);
            if (before == null)
            {
                return 0f;
            }
            return Vector3.Distance(before.Value, victim.transform.position);
        }

        private void Flag(SteamPlayer suspect, string report)
        {
            Logger.Log("[FLAG] " + report);

            if (Configuration.Instance.NotifyAdmins)
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    if (client.isAdmin)
                    {
                        ChatManager.serverSendMessage("[AntiLag] " + report, Color.red,
                            null, client, EChatMode.SAY, null, true);
                    }
                }
            }

            string webhookUrl = Configuration.Instance.DiscordWebhookUrl;
            if (string.IsNullOrEmpty(webhookUrl))
            {
                return;
            }
            suspect.player.sendScreenshot(CSteamID.Nil,
                (steamId, jpg) => DiscordWebhook.Send(webhookUrl, report, jpg));
        }

        private PlayerLagState GetState(CSteamID steamId)
        {
            if (!States.TryGetValue(steamId, out PlayerLagState state))
            {
                state = new PlayerLagState();
                States[steamId] = state;
            }
            return state;
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            States.Remove(player.CSteamID);
        }
    }
}
