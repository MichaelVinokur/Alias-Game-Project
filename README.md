# Alias – Multiplayer Mobile Word Game (Unity)

A mobile version of the party word game **Alias**, built in **Unity (C#)** for Android and iOS. Teams take turns explaining words against the clock, and the game handles the rounds, the timer, the scoring and the multiplayer lobby.

**Tech:** Unity 6 · C# · Photon PUN 2 (multiplayer networking) · TextMesh Pro + RTL support · Android / iOS

---

## Features
- **Multiplayer lobby:** players in the same room join a shared game session over **Photon** networking, and the game recovers and rejoins if a player's mobile connection drops.
- **Teams and turn-based rounds:** players are split into teams, play alternating turns, and get a scoreboard after each round.
- **Real-time timer** for each turn.
- **Word categories:** players choose categories, and words are randomized from text-based word banks.
- **Hebrew support:** right-to-left text rendering (RTLTMPro), with cleanup of invisible text-direction characters in the word lists.
- Background music with a mute toggle, a loading screen and a layout that fits notched phones (safe area).

## Project structure
```
Assets/
 ├─ scripts/     # game logic: GameManager, RoundManager, ScoreboardManager,
 │               # WordBank, SameRoomNetworkManager, NetworkGuardian, ...
 ├─ Scenes/      # MainMenu, SameRoom lobby, Game, Round, Scoreboard, EndGame
 ├─ Resources/   # word lists by category
 └─ Prefabs/, Audio/, arts/
```

## Run it
1. Open the project in **Unity 6000.0.61f1** or a later Unity 6 version.
2. Open `Assets/Scenes/MainMenu.unity` and press Play, or build for Android or iOS.

---
*Team project – Topics in Geometric Networks course, Ben-Gurion University.*
