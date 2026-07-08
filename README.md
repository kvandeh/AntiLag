# AntiLag

Unturned RocketMod plugin that detects **lagswitching** and flags the suspect.

Lagswitching: a player briefly cuts their outgoing packets. On their screen the fight
continues, but to everyone else they freeze — and to *them* their target keeps moving
predictably while the server hears nothing. When packets resume, their queued shots land
against targets that had no chance to react. This plugin makes that visible.

**v1 flags only. It never bans or kicks.**

## How it works

- The server processes one input frame per packet batch a client sends
  (`PlayerInput.onPluginKeyTick`). AntiLag timestamps these per player; a real-time gap
  above the threshold is recorded as a *lag moment*.
- Every half second, every player's position is cached (last ~12 seconds).
- When player A damages player B, AntiLag checks whether A just came out of a lag moment
  (or is under the game's own fake-lag penalty). If so, the hit is **flagged**: console
  log + message to online admins, including the gap length and how far B moved while A
  was dark — the evidence that A shot at a stale position.
- Optionally, flagged hits deal reduced damage (`DamageMultiplier`), stacking with the
  game's built-in 0.1× fake-lag penalty for gaps over `Fake_Lag_Threshold_Seconds`.

## Configuration

| Setting | Default | Meaning |
|---|---|---|
| `GapThresholdSeconds` | `0.5` | Packet gap counted as a lag moment |
| `SuspicionWindowSeconds` | `3` | Damage within this window after a gap is flagged |
| `DamageMultiplier` | `1.0` | Damage scale on flagged hits (`1.0` = flag only) |
| `NotifyAdmins` | `true` | Send flag messages to online admins |
| `DiscordWebhookUrl` | *(empty)* | If set, flags post to Discord as an embed with a live `/spy` screenshot of the suspect attached |

## Commands

| Command | Permission | Description |
|---|---|---|
| `/antilag` | `antilag` | List flagged players with strike counts this session |

## Install

1. `dotnet build -c Release` (or grab `AntiLag.dll` from the latest GitHub Release)
2. Copy `AntiLag/bin/Release/net48/AntiLag.dll` to `Servers/<server>/Rocket/Plugins/`
3. Restart or `/rocket reload`

## Testing

On a test server with two clients: stand still with client A aiming at moving client B,
suspend A's connection ~1 s (clumsy: unplug ethernet; controlled: NetLimiter/clumsy drop
outbound), release, shoot B. Expect a `[AntiLag]` flag in server console naming A, the
gap duration, and B's displacement.