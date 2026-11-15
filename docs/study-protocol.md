# Study protocol: Depth Guides pilot

## Research question
Do simple visual depth guides (a vertical pole and a floor shadow) help people place objects more accurately in a 3D scene shown on a flat screen, and do they help people with no 3D experience more than people with 3D experience?

## Why it matters
On a flat screen, depth is the hardest dimension to judge: left/right and up/down are visible directly, but near/far has to be inferred from cues such as size, perspective and shadows. People who have never used 3D software or games have had less practice reading those cues. If XR and 3D tools are to reach first-time users, including people in low-resource settings, interfaces need to support depth judgement without getting in the way.

## Hypotheses
- **H1.** Placement error will be larger in **depth** (near/far) than in the **side** (left/right) or **height** direction.
- **H2.** With guides ON, depth error will be lower than with guides OFF.
- **H3.** The benefit of the guides will be larger for participants **without** 3D experience.
- **H4.** Error will increase for targets that are further away.

## Design
- **Within-subjects:** every participant plays both conditions (guides ON and guides OFF), 5 targets each.
- **Counterbalancing:** a coin flip decides which condition comes first, so learning across the session does not favour one condition.
- **Between-subjects factor:** 3D experience (self-reported yes/no).
- **Fixed targets:** two fixed sets of 5 target positions, so every participant faces the same targets.
- **Controlled cues:** real-time shadows are switched off, because shadows are themselves a depth cue. The room (walls, floor, objects) is the same in both conditions.

## Measures (recorded for every placement)
| Measure | Meaning |
|---|---|
| `depth_error` | Distance between cube and target along the near/far axis |
| `side_error` | Distance along the left/right axis |
| `height_error` | Distance along the up/down axis |
| `total_error` | Straight-line distance between cube and target |
| `time_s` | Seconds from the target appearing to pressing PLACE |
| `target_depth` | Distance from the camera to the target |

Also recorded per participant: name, 3D experience (yes/no), which condition came first, and which round felt easier (with guides / without guides / same).

## Procedure
1. Participant opens the game on a phone (held sideways) or laptop.
2. Enters their name and answers the 3D experience question.
3. Plays round 1 (5 targets), a short break screen, then round 2 (5 targets).
4. Answers which round felt easier.
5. Screenshots the results screen and sends it to the researcher.

Session length: about 5 minutes.

## Participants (pilot)
A small convenience sample (about 4 to 10 people), mixing people with and without 3D experience.

## Limitations
- Small sample: results show trends, not proof.
- Phones differ in screen size, which may affect performance.
- 3D experience is self-reported as a simple yes/no.
- Informal pilot: no ethics approval, so data will not be published as a formal study.

## Next steps
- Test with a larger and more varied group, including first-time computer users.
- Compare more guide designs (for example, a floor grid or a depth meter).
- Run the same task in VR, where stereo vision adds another depth cue.
