# The Reach Screensaver v1.0.0 — Destinations & Approximate Voyage Times

> **Route:** 41-stop Solar System journey · **Release:** [V1.0.0](https://github.com/NoReturns/TheReachScreensaver/releases/tag/V1.0.0) · **Route ID:** `solar-system-41`

## Quick overview

- **Starting point:** Earth. Stops are visited in the numbered order below.
- **Full circuit:** approximately **4:10:31** of active playback at the midpoint of the production timing ranges, from the start at Earth through all 41 stops and arrival back at Earth.
- **Possible circuit duration based on configured phase limits:** **1:49:34 to 6:31:27**. These are theoretical end-to-end sums of minimum/maximum phase durations, not promises or observed benchmarks.
- **After Venus:** the route returns to Earth and repeats in the same forward order. Reverse travel, interstellar destinations and a true out-and-back journey are not in v1.0.0.
- **Important:** Time only advances while the voyage is running; the screensaver resumes saved progress across sessions. These are **not wall-clock ETAs** measured from an install date.

## Current 41-stop route

**Reading the table:** “Travel in” is the randomly selected cruise duration **from the previous stop** to this destination (it does not include departure). “Observation” is time spent viewing the destination. “Approx. arrival” is the cumulative active playback time from a fresh start at Earth, using the midpoint of each timing range. Departure adds **10 seconds**, or **7 seconds when leaving a moon**, and is included in the approximate arrival calculation. Round-off is to the nearest second.

| # | Destination | Travel in (range) | Observation (range) | Approx. arrival* |
|---:|---|---:|---:|---:|
| 1 | Earth | Start | 30s–2m | 0:00 |
| 2 | Moon | 45s–3m | 12s–30s | 3:18 |
| 3 | Mars | 2m–10m | 30s–2m | 9:46 |
| 4 | Phobos | 45s–3m | 12s–30s | 13:03 |
| 5 | Deimos | 45s–3m | 12s–30s | 15:24 |
| 6 | Vesta | 4m–15m | 20s–1m 15s | 25:22 |
| 7 | Ceres | 4m–15m | 30s–2m | 35:49 |
| 8 | Jupiter | 2m–10m | 30s–2m | 43:14 |
| 9 | Io | 45s–3m | 12s–30s | 46:32 |
| 10 | Europa | 45s–3m | 12s–30s | 48:52 |
| 11 | Ganymede | 45s–3m | 12s–30s | 51:13 |
| 12 | Callisto | 45s–3m | 12s–30s | 53:33 |
| 13 | Saturn | 2m–10m | 30s–2m | 1:00:01 |
| 14 | Mimas | 45s–3m | 12s–30s | 1:03:19 |
| 15 | Enceladus | 45s–3m | 12s–30s | 1:05:39 |
| 16 | Tethys | 45s–3m | 12s–30s | 1:08:00 |
| 17 | Dione | 45s–3m | 12s–30s | 1:10:20 |
| 18 | Rhea | 45s–3m | 12s–30s | 1:12:41 |
| 19 | Titan | 45s–3m | 12s–30s | 1:15:01 |
| 20 | Iapetus | 45s–3m | 12s–30s | 1:17:22 |
| 21 | Uranus | 4m–15m | 30s–2m | 1:27:20 |
| 22 | Miranda | 45s–3m | 12s–30s | 1:30:37 |
| 23 | Ariel | 45s–3m | 12s–30s | 1:32:58 |
| 24 | Umbriel | 45s–3m | 12s–30s | 1:35:18 |
| 25 | Titania | 45s–3m | 12s–30s | 1:37:39 |
| 26 | Oberon | 45s–3m | 12s–30s | 1:39:59 |
| 27 | Neptune | 4m–15m | 30s–2m | 1:49:57 |
| 28 | Triton | 45s–3m | 12s–30s | 1:53:15 |
| 29 | Pluto | 4m–15m | 30s–2m | 2:03:13 |
| 30 | Charon | 45s–3m | 12s–30s | 2:06:30 |
| 31 | Haumea | 4m–15m | 30s–2m | 2:16:28 |
| 32 | Makemake | 4m–15m | 30s–2m | 2:27:23 |
| 33 | Eris | 4m–15m | 30s–2m | 2:38:18 |
| 34 | Pioneer 11 | 5m–15m | 1m–3m | 2:49:43 |
| 35 | Pioneer 10 | 5m–15m | 1m–3m | 3:01:53 |
| 36 | Voyager 2 | 5m–15m | 1m–3m | 3:14:03 |
| 37 | Voyager 1 | 5m–15m | 1m–3m | 3:26:13 |
| 38 | Halley's Comet | 4m–15m | 20s–1m 15s | 3:37:53 |
| 39 | Sun | 4m–15m | 40s–1m 40s | 3:48:21 |
| 40 | Mercury | 2m–10m | 30s–2m | 3:55:41 |
| 41 | Venus | 2m–10m | 30s–2m | 4:03:06 |

\* Approx. arrival is the **midpoint estimate**, not a fixed arrival time. The actual elapsed time accumulates random phase durations and may differ considerably, especially later in the route.

**Completing the loop:** after observing **Venus**, the screensaver spends another **10 seconds departing** and then **2–10 minutes cruising** to Earth. Expected repeat arrival: **4:10:31** after a fresh start (range **1:49:34–6:31:27**).

## Production timing rules

| Phase / target type | Time range |
|---|---:|
| Cruise to a moon | 45 seconds–3 minutes |
| Cruise to an ordinary planet | 2–10 minutes |
| Cruise to distant destinations | 4–15 minutes |
| Cruise to an historic spacecraft (probe) | 5–15 minutes |
| Observation of planets / dwarf planets | 30 seconds–2 minutes |
| Moon flyby / observation | 12–30 seconds |
| Small body / comet observation | 20–75 seconds |
| Spacecraft observation | 1–3 minutes |
| Sun observation | 40–100 seconds |
| Departure from a moon | 7 seconds |
| Departure from any other stop | 10 seconds |

**Distant-cruise destinations in this release:** Vesta, Ceres, Uranus, Neptune, Pluto, Haumea, Makemake, Eris, Halley’s Comet, and the Sun. This is a presentation-timing classification, not a statement that their actual physical distance is constant.

Each range is sampled when its phase starts, and the sampled duration is retained in the saved journey. Midpoint calculations are an **illustration**, not a timetable or a prediction of how long a particular user will take to complete a voyage.

## Source of truth

This list is drawn from the public **v1.0.0 release-source revision** `cf7567ca5ed89c174cdc22986eb000b3a2cdbc3a`:

- [41-stop itinerary and forward-repeat definition](https://github.com/NoReturns/youreyesinspace/blob/cf7567ca/src/reach/ReachVoyageDirector.ts)
- [Production time ranges, travel classification and phase sampling](https://github.com/NoReturns/youreyesinspace/blob/cf7567ca/src/reach/ReachTiming.ts)
- [Journey itinerary version and persistence schema](https://github.com/NoReturns/youreyesinspace/blob/cf7567ca/src/reach/JourneyState.ts)

**Calculation:** For each destination after Earth, estimated arrival time adds the midpoint of the previous stop’s observation, the previous stop’s fixed departure, and the midpoint of the inbound cruise duration. The full loop additionally includes the observation at Venus, its 10-second departure, and the final 2–10-minute cruise to Earth.
