# Depth Guides

**Can one simple visual cue help first-timers judge depth in 3D?**

▶ **Play it:** https://mmeso-iconic.github.io/depth-guides (hold your phone sideways, or use a laptop)

People new to 3D often misjudge depth: things are further or closer than they look. Most 3D tools are designed by people who already think in 3D, so beginners get left behind. Depth Guides is a small Unity experiment that tests whether a simple visual cue can close that gap.

## How it works
- The player moves an **orange cube** to the exact spot of a **teal ball** inside a room, then presses **PLACE**.
- There are two rounds of 5 targets. In one round the **guides are ON**: a pole down to the floor and a floor "shadow" under each object. In the other round they are **OFF**.
- A coin flip decides which round comes first, so practice does not favour one condition.
- Every player gets the same fixed targets. Real-time shadows are turned off, because shadows are themselves a depth cue.
- For every placement the app records the error split into **depth** (near/far), **side** (left/right) and **height**, plus time. Players also say whether they have 3D experience and which round felt easier.

## Controls
- **Phone / touch:** on-screen LEFT, RIGHT, AWAY, NEAR, UP, DOWN and PLACE buttons
- **Keyboard:** W/A/S/D move, E/Q up/down, Space place

## Repository
| Folder | What is inside |
|---|---|
| `unity/` | The experiment code (`DepthTask.cs`) and a small browser plugin for the phone keyboard |
| `docs/study-protocol.md` | Research question, hypotheses, measures and procedure |
| `docs/participant-script.md` | What I say to each participant, step by step |
| `docs/pilot-results.md` | Findings from the pilot test (filled in after testing) |
| `data/` | Template for recording results |
| `analysis/analyze.py` | Script that summarises the results and draws a chart |
| `index.html`, `Build/`, `TemplateData/` | The playable web build |

## Run it in Unity
1. Create a Unity 6 project with the **Universal 3D** template.
2. **Edit > Project Settings > Player > Other Settings > Active Input Handling = Both.**
3. Copy `unity/DepthTask.cs` into `Assets/` and `unity/Plugins/NamePrompt.jslib` into `Assets/Plugins/`.
4. Attach `DepthTask` to one empty GameObject and press Play.

## Background
Inspired by research on non-intrusive visual guides for immersive 3D drawing and on how spatial ability affects performance in 3D. This is an informal pilot, not a formal study: no ethics approval was sought, so results are for learning and design only.

Built by [Mmesoma Ugwuanyi](https://mmeso-iconic.github.io).
