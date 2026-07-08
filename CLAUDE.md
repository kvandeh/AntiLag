# AntiLag

RocketMod plugin for Unturned that detects lagswitching (a player briefly stops sending
packets so opponents freeze on their screen, giving them easy shots) and **flags** the
suspect. It never bans; punishment beyond optional damage scaling is the admin's call.

## Build

```
dotnet build -c Release
```

Output: `AntiLag/bin/Release/net48/AntiLag.dll` → drop into `Servers/<server>/Rocket/Plugins/`.

## Stack

- .NET Framework 4.8, SDK-style csproj (pattern copied from RestoreMonarchyPlugins).
- Single NuGet dependency: `RestoreMonarchy.RocketRedist` (`ExcludeAssets="runtime"`) —
  provides Rocket API + Unturned + UnityEngine reference assemblies at compile time.
- No tests project; the plugin is validated on a live server (see README "Testing").

## How detection works

1. **Packet heartbeat** — `PlayerInput.onPluginKeyTick` fires server-side once per
   processed client input frame. Real-time gap between successive ticks for a player is
   the packet gap. Gap ≥ `GapThresholdSeconds` → record a lag moment for that player.
2. **Position cache** — every 0.5 s snapshot every player's position into a per-player
   ring buffer (~12 s of history). Used as evidence: how far did the *victim* move while
   the attacker's packets were dark.
3. **Damage correlation** — `DamageTool.damagePlayerRequested`: if the attacker had a lag
   moment ending less than `SuspicionWindowSeconds` ago (or the game's own
   `PlayerInput.IsUnderFakeLagPenalty` is set), flag: console log + admin chat message,
   strike count incremented, and optionally `parameters.times *= DamageMultiplier`.

## Context: the game's built-in penalty

Unturned itself silently multiplies damage by 0.1 while a player is under "fake lag
penalty" (input gap > max(1 s, server config `Fake_Lag_Threshold_Seconds`)). It logs only
if `Fake_Lag_Log_Warnings` is on and gives admins no flagging/telemetry. This plugin
complements it: visibility, strike counts, sub-1-second gaps, victim-movement evidence.
Don't reimplement the penalty — read `IsUnderFakeLagPenalty` as a strong confirm signal.

## Layout

```
AntiLag/
  AntiLagPlugin.cs         plugin: event wiring, snapshot loop, flag logic
  AntiLagConfiguration.cs  Rocket XML config (thresholds, multiplier, notify)
  PlayerLagState.cs        per-player data: last input time, last gap, positions, strikes
  AntiLagCommand.cs        /antilag — list flagged players with strike counts
```

## Conventions

- Simplicity over all: no abstraction until three concrete uses, guard clauses over
  nesting, a class earns its file or it doesn't exist.
- Everything runs on Unity's main thread (Rocket events, `onPluginKeyTick`, `Update`) —
  no locks.
- Per-player state lives in one `Dictionary<CSteamID, PlayerLagState>`, cleaned up on
  disconnect.
- Flag-only philosophy: false positives are indistinguishable from real packet loss, so
  the plugin reports and (optionally) softens damage — it never punishes on its own.
