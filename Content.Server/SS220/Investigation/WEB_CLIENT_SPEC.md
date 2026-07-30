# Investigation Web Client — Specification

Reader spec for the bundles produced by `InvestigationRecorder`. Schema version **1**.

The producer is the authority on the format; every field below is emitted by
`InvestigationRecorder.cs` / `InvestigationRecorderSystem.cs`. If they disagree, the code wins and this
document is stale.

---

## 1. What this is

A schematic, scrubbable map + timeline of a single round, for admins investigating complaints.

**In scope:** where every tracked character was at any moment, the station geometry around them at that
moment, what they were carrying, what they said, and every admin-log event pre-joined to a position.

**Explicitly out of scope:**

- **Visual fidelity.** No sprites, lighting, atmospherics, or animation. Dots, lines, and tiles. If someone
  needs to *see* the round, they open the engine replay.
- **Player history / moderation records.** Playtime, notes, prior bans, character lists — these are
  account-level and already live in the existing admin panel. Link out to it; do not rebuild it.
- **Verdicts.** This is evidence gathering. A large share of real complaints turn on RP-quality judgement
  that no dataset resolves.

---

## 2. Bundle layout

One directory per round:

```
investigation/round-<roundId>_<yyyy-MM-dd_HH-mm-ss>/
├── meta.json              # plain JSON, not compressed
├── roster.jsonl.gz
├── control.jsonl.gz
├── positions.jsonl.gz
├── navmap.jsonl.gz
├── characters.jsonl.gz
├── chat.jsonl.gz
├── health.jsonl.gz
└── events.jsonl.gz
```

`*.jsonl.gz` is gzipped newline-delimited JSON: one complete JSON object per line, no enclosing array,
trailing newline after each row. Decompress in-browser with `DecompressionStream('gzip')`.

Two robustness requirements for readers, both learned from real bundles:

- **Tolerate a leading byte order mark.** Bundles written before the recorder switched to BOM-less UTF-8
  begin every stream — and `meta.json` — with U+FEFF, which makes the first line invalid JSON. Those
  files still exist, so strip it if present.
- **Tolerate a truncated final member.** A server that dies mid-round leaves an unterminated gzip stream
  and possibly a half-written last line. Keep what parsed and carry on; do not fail the whole round.

Rows within a file are **append-ordered, therefore non-decreasing in `t`**. Do not assume strict
monotonicity — several rows commonly share a tick.

### 2.1 Field conventions

| Key | Meaning |
|---|---|
| `t` | Absolute game tick (uint). Seconds since recording start = `(t - meta.startTick) / meta.tickRate`. |
| `e` | Entity id (int). This is the server-side `EntityUid`, a per-round monotonic counter that is never recycled — it is the join key across every stream, and the same value admin logs record. |
| `g` | Grid entity id (int), or `null` when the entity is off-grid (in space, or parented to the map). |
| `c` | Container entity id (int) — the thing the entity is *inside* (locker, crate, body bag, mech). |
| `x`, `y` | **Grid-local** coordinates in tiles. One decimal place in `positions`, two everywhere else (see §3.2). |

**Coordinates are grid-local, not world.** This is deliberate: a character standing on the evac shuttle
keeps stable coordinates whether it is docked or in flight, so tracks do not smear when a grid moves.
The corollary is that **you may only render entities and navmap chunks that share the same `g` in one
coordinate frame.** Render one grid at a time (see §5.1).

`null`-valued fields are **omitted entirely** in `characters` and `events` rows (the serializer skips
nulls). The one exception is `positions`, where `g` is always present and may be literal `null`. Treat
absent and null as equivalent everywhere.

---

## 3. Stream schemas

### 3.1 `meta.json`

```json
{
  "schema": 1,
  "roundId": 4242,
  "map": "TestStation",
  "serverName": "ss220-main",
  "startedUtc": "2026-07-29T14:03:11.4820000Z",
  "startTick": 118000,
  "endTick": 334120,
  "tickRate": 30,
  "durationSeconds": 7204.5,
  "languages": [
    { "id": "Galactic", "key": "%g", "name": "Общегалактический" },
    { "id": "Codespeak", "key": "%c", "name": "Кодспик", "color": "#8F4A4BFF" }
  ],
  "roster": [
    {
      "e": 4821,
      "player": "a3f2c1d0-...",
      "userName": "Kypatop",
      "name": "Urist McGriff",
      "prototype": "MobHuman",
      "firstTick": 118420
    }
  ]
}
```

`meta.json` is written **twice**: once at round start, and again on a clean stop. A bundle from a
crashed server therefore still has a readable `meta.json`, but with **`durationSeconds` and a final
`endTick` absent**. Treat a missing `durationSeconds` as "this round did not end cleanly" — the streams
are still valid up to wherever they stopped.

`languages` is every language prototype that was loaded, with the `%key` prefix that selects it in chat,
its localized name and its in-game colour (`RRGGBBAA`, absent when the language does not recolour speech).
It is here so a reader can resolve the prefixes embedded in `chat.jsonl.gz`'s `msg` without hardcoding a
copy of the game's prototypes; see §3.4a. Absent on bundles recorded before this field existed.

`roster` is every entity that was *ever* player-controlled this round. Entities stay on the roster after
the player leaves them — tracking a corpse being dragged away is intentional and often the point.

Because the `roster` array here is only complete on a clean stop, prefer merging it with
`roster.jsonl.gz` (§3.2), which is appended as players are tracked and survives a crash.

### 3.1a `roster.jsonl.gz`

```json
{"e":4821,"player":"a3f2c1d0-…","userName":"Kypatop","name":"Urist McGriff","prototype":"MobHuman","firstTick":118420}
```

One row per tracked entity, appended at the moment it is first player-controlled. Identical fields to a
`meta.roster` element. Union the two, keyed on `e`.

`player` is the account GUID and `userName` the login; both may be absent for entities that were
tracked without a session. **`name` is the character name at the moment of first attachment** and is not
updated — for the current name at time `t`, read the `characters` stream.

**`player` here is the *first* account to control the entity, and is not the answer to "who was driving
this at time `t`."** Use `control.jsonl.gz` (§3.1b) for that. Attributing a whole round of a body's
actions to whoever held it first is wrong the moment it is cloned, borged, revived by someone else, or
possessed by an admin.

**Ghosts are not on the roster.** Entities carrying `GhostComponent` are excluded entirely — no roster
entry, no positions, no control rows — because they pass through walls, exist in numbers late in a round,
and cannot act on anything. Ghost-like *mobs* that can act, notably the revenant, are ordinary roster
members and appear as usual.

### 3.1b `control.jsonl.gz`

```json
{"t":118420,"e":4821,"player":"a3f2c1d0-…","userName":"Kypatop","action":"attach"}
{"t":141902,"e":4821,"player":"a3f2c1d0-…","userName":"Kypatop","action":"detach"}
{"t":142310,"e":4821,"player":"7d90bb31-…","userName":"Someone","action":"attach"}
```

One row every time a player takes control of an entity or lets go of it. `action` is `attach` or
`detach`. This is the stream that answers **"who was driving this body at tick `T`"**: fold it per entity
into intervals, and the controller at `T` is whoever attached most recently without a later detach.

An entity with no `attach` covering `T` was unpiloted then — a corpse, an SSD body, a borg with nobody in
it. That is a meaningful state and worth rendering distinctly rather than falling back to the roster
entry.

Rows are cheap and rare: a quiet round produces roughly one pair per player. `player`/`userName` are
absent for control changes with no session behind them.

### 3.2 `positions.jsonl.gz`

```json
{"t":118432,"e":4821,"g":17,"x":42.1,"y":-17.3}
{"t":118438,"e":4821,"g":17,"x":42.4,"y":-17.1,"c":9102}
```

Sampled at `investigation.position_interval` (default 0.5s = 2Hz), then filtered twice before a row is
written:

1. **Movement.** Nothing is emitted unless the entity moved more than `investigation.position_epsilon`
   (default 0.15 tiles), or `g`/`c` changed. A stationary character costs nothing.
2. **Dead reckoning.** A sample that lies on the straight line between the previous emitted row and the
   next sample — within the same epsilon — is dropped, because a reader can reconstruct it. Movement in
   SS14 is mostly straight lines down corridors, so this removes the large majority of remaining rows.

> **A row is a vertex of a path, not a snapshot of a moment.** This is the one thing about this stream
> that a reader must get right. Stepping to "the last row at or before `T`" makes entities teleport
> between corners instead of walking to them. Interpolate between the row at or before `T` and the next
> row, by tick.

Coordinates carry **one decimal place**. The epsilon below which movement is not recorded at all is 0.15
tiles, so a second decimal would encode only sampling jitter, on every row of by far the largest stream.

Even after both filters this stream outweighs every other one by two orders of magnitude, so it is the
only one whose size matters. A reader that is slow or memory-hungry will be slow here and nowhere else.

Consequences the reader must handle:

- **Rows are sparse and irregular.** A stationary character emits nothing for minutes, and a walking one
  emits only at direction changes. To get a position at arbitrary tick `T`, binary-search that entity's
  rows for the last row with `t <= T`, then interpolate towards the next row.
- **A change in `g` or `c` is a discontinuity, not movement.** Never interpolate across one. Break the
  track: walking from station onto a shuttle, or being stuffed into a locker, makes coordinates jump to a
  different frame. Interpolating produces a fictional path straight through walls. The recorder applies
  the same rule and never elides a sample across a discontinuity, so the two ends always agree.
- **A repeated position at a later tick is a deliberate anchor, not a duplicate.** When an entity stops and
  later moves off again, the recorder re-emits the position it was holding, dated at the last tick it was
  still there. Without it the interpolation would run from wherever the previous row was — possibly minutes
  earlier — straight to the next one, and a character who stood still would render as slowly drifting. Do
  not deduplicate consecutive rows with equal coordinates; the tick is the payload.
- **An entity with no row at all before `T` did not exist / was not tracked yet.** Render nothing.
- **`c` present means the entity is inside something.** Its `x`/`y` are the *container's* position. Render
  it as inside-a-thing (badge, stacked marker) rather than a bare dot — "the body was in that locker" is
  a distinct fact from "the body was at those coordinates".

Setting `investigation.position_epsilon` to 0 disables both filters and restores a plain sample per
interval. How much larger that is depends entirely on how much the crew moved — measured on two recorded
rounds, dead reckoning alone removed 31% of rows on one and 96% on the other. Nothing but debugging
should need it.

### 3.3 `navmap.jsonl.gz`

Two row shapes in one stream. Discriminate on the presence of `tiles` vs `beacons`.

**Chunk row:**

```json
{"t":118400,"g":17,"cx":3,"cy":-2,"tiles":[0,240,3855,...]}
```

`tiles` is always exactly **64** ints — an 8×8 chunk. `cx`/`cy` are the chunk origin in chunk units.

Chunks are emitted as a full snapshot on first sight of a grid, then **only when dirty**. To get the map
at tick `T`, replay every chunk row with `t <= T` into a `Map<gridId, Map<"cx,cy", tiles>>`, last write
winning. This is a fold, not a lookup.

**Tile decoding** (constants from `SharedNavMapSystem`):

```js
const CHUNK = 8;
const FLOOR_MASK   = 0x00F;  // bits 0-3
const WALL_MASK    = 0x0F0;  // bits 4-7
const AIRLOCK_MASK = 0xF00;  // bits 8-11

// index -> position within chunk. Note this is COLUMN-major: index = x * 8 + y.
const x = Math.floor(i / CHUNK);
const y = i % CHUNK;

// absolute tile coords in the grid's local frame
const tileX = cx * CHUNK + x;
const tileY = cy * CHUNK + y;

const hasFloor   = (tiles[i] & FLOOR_MASK)   !== 0;
const hasWall    = (tiles[i] & WALL_MASK)    !== 0;
const hasAirlock = (tiles[i] & AIRLOCK_MASK) !== 0;
```

Each category carries 4 direction bits. Presence testing (mask non-zero) is sufficient for v1; the
direction bits exist for finer edge rendering later.

Coverage caveat: navmap only classifies **walls and airlocks**. Tables, lockers, windows, machines, and
floor *type* are not in it. This is the same abstraction the in-game station map uses, which is a feature
— it matches the mental model admins already have.

**Beacon row:**

```json
{"t":118400,"g":17,"beacons":[{"name":"Medbay","x":42.0,"y":-17.0,"color":"3FA9F5FF"}]}
```

Beacons are the station's room labels. `color` is RRGGBBAA hex, no leading `#`. Each row is the
**complete** beacon set for that grid at that tick — replace, do not merge.

These give you room names for free: nearest-beacon to a position answers "which room was this" with no
rendering at all. Use it for text output ("Medbay, 14:32:07"), which is most of what an investigation
actually needs.

### 3.4 `characters.jsonl.gz`

```json
{
  "t":118400, "e":4821, "name":"Urist McGriff",
  "species":"Human", "gender":"Male", "age":34,
  "job":"SecurityOfficer", "department":"Security", "departmentColor":"DE3A3AFF",
  "access":["Armory","Security"],
  "worn":{"outerClothing":"ClothingOuterHardsuitSecurity","id":"SecurityPDA"},
  "hands":["WeaponPistolMk58"],
  "carried":["Handcuffs","Flashlight"]
}
```

Emitted only when the loadout **fingerprint changes** (checked every
`investigation.character_interval`, default 10s). So:

- Rows are sparse and bursty. A passenger who never changes clothes has exactly one row all round.
- **Up to ~10s stale.** Fine for "did they have armory access around then", not for exact-tick inventory.
  Do not present it as tick-precise.
- Same lookup pattern as positions: last row with `t <= T`.

`access` is the *effective* access resolved through held items, the ID slot, and PDAs — not just what is
printed on one card. `worn` maps slot id → entity prototype. Values throughout are prototype IDs, falling
back to the entity name when an entity has no prototype. `carried` is storage contents to
`investigation.storage_depth` (default 2 — the bag and what is in it).

`department` and `departmentColor` are resolved from `job` server-side, against the same
`DepartmentPrototype` set the game uses, so a reader gets the canonical grouping and palette without
reimplementing the job→department mapping or inventing its own colours. `departmentColor` is RRGGBBAA hex
with no leading `#`, matching the beacon convention. Both are absent when the entity has no job — a
passenger, a monkey, an unassigned corpse.

### 3.4b `health.jsonl.gz`

```json
{"t":118432,"e":4821,"dmg":37.5,"state":"Alive","crit":100.0,"dead":200.0}
```

| Field | Meaning |
|---|---|
| `dmg` | Total damage across every type, rounded to 2 decimals. |
| `state` | `MobState` name: `Alive`, `Critical`, `Dead`, or `Invalid`. |
| `crit` | Damage at which this entity goes into crit. Absent when it has no such threshold. |
| `dead` | Damage at which this entity dies. Absent when it has no such threshold. |

Sampled from the position loop, so it inherits `investigation.position_interval` (default 0.5s) rather
than the much slower character interval — health during a fight changes in well under a second, and a 10s
snapshot would miss the whole thing.

A row is written **only when `dmg` or `state` changed**, so an unharmed character costs one row for the
round. Same lookup pattern as positions and characters: last row with `t <= T`. Entities with no
`DamageableComponent` never appear at all.

`crit`/`dead` ride along on every row so a reader can render a bar as a fraction rather than an opaque
number — they are per-entity constants in practice, not a time series.

Deliberately reduced to one number: the per-type breakdown (brute vs burn vs asphyxiation) is not here,
because it is already in `events.jsonl.gz` as `Damaged` log rows. This stream exists to draw a bar and to
mark the tick somebody went critical.

### 3.4a `chat.jsonl.gz`

Everything anyone said, with the text as typed.

```json
{"t":118432,"e":4821,"ch":"Say","name":"Urist McGriff","msg":"привет, мир","lang":"Galactic","g":17,"x":42.1,"y":-17.3}
{"t":118440,"e":4821,"ch":"Radio","name":"Urist McGriff","msg":"code red","lang":"Galactic","rc":"Security"}
{"t":118450,"ch":"OOC","name":"Kypatop","msg":"ooc line"}
```

| Field | Meaning |
|---|---|
| `e` | Speaking entity. **Absent** for channels with no in-world speaker (`OOC`, `AdminChat`). |
| `ch` | `Say`, `Whisper`, `Radio`, `Emote`, `LOOC`, `Dead`, `OOC`, `AdminChat`. |
| `name` | Displayed name at the time, which may be a **disguised identity** (voice mask, agent ID). |
| `msg` | The original text. Not language-obfuscated, not accent-transformed, not markup-wrapped. **`%key` language prefixes are left in.** |
| `lang` | The speaker's *selected* language (`Galactic`, `SolCommon`, `Codespeak`, …) — what the un-prefixed part of `msg` was spoken in. Absent on channels that carry no language (`Emote`, `LOOC`, `OOC`) and on bundles recorded before this field existed. |
| `langs` | Present **only** when one line mixed two or more languages, as the full ordered list of what the sanitizer found. |
| `rc` | Radio channel prototype id (`Security`, `Common`, `Syndicate`, …). Present only on `ch == "Radio"`. |
| `g`/`x`/`y`/`c` | Speaker position, inline. Absent when the speaker had no sampled position. |

Two things worth knowing:

**Why this exists at all.** Chat also appears in `events.jsonl.gz` as `LogType.Chat`, but only as a
finished sentence — `"Say from X (uid/nuid, proto, user): TEXT, defaultLanguage: Galactic."` The text is
not a field there, because
`Content.Shared/Administration/Logs/LogStringHandler.cs`'s `AppendFormatted(string?)` overload does not
call `AddFormat`, so bare interpolation holes never reach the structured values. Recovering the words
would mean parsing ~10 localized per-channel shapes whose text can itself contain `: ` and `, `.

**Positions are inline on purpose.** Drawing a speech bubble needs no join against the position stream.

Rows are tick-ascending, so a bubble layer can binary-search to the playhead and walk backwards until
outside its display window.

**Language is recorded as three separate facts, not one.** SS14 encodes language in the text itself: a
`%key` prefix switches language part-way through a sentence, and everything before the first prefix is in
whatever the speaker had selected. Flattening that to a single "the language of this line" would throw
away the thing an investigator most wants to see. So the bundle keeps all of it: `lang` is the selected
default, `msg` keeps its prefixes so the runs can be recovered, and `langs` flags the lines where doing so
is actually necessary. The key → language table is in `meta.json` (§3.1), so a reader never has to carry
its own copy of the prototypes.

**Out-of-character channels are included, and two of them are sensitive.** `Dead` carries a speaking
entity (the ghost), so it still gets a position resolved live; `AdminChat` has no in-world speaker at all.
Both are recorded because metacommunications complaints are the single most common thing that cannot be
resolved from in-character channels alone. Anyone distributing a bundle outside the admin team should
strip `ch` in `{"OOC","LOOC","Dead","AdminChat"}` first — the engine draws the same line with
`replay.record_admin_chat`, which defaults to off for exactly this reason.

**Deliberate duplication.** The same message exists in both `chat` and `events`. That keeps the rule
"`events` is the complete admin log stream, nothing removed" while giving readers clean text. A viewer
showing both should filter `type == "Chat"` out of its admin-log view, or every line appears twice.

### 3.5 `events.jsonl.gz`

Every admin log, in order, pre-joined to positions.

```json
{
  "t":118432,
  "utc":"2026-07-29T14:32:07.1230000Z",
  "type":"MeleeHit",
  "impact":"High",
  "msg":"Urist McGriff (4821/n4821, MobHuman, Kypatop) melee attacked (light) John Victim ... and dealt 32 damage",
  "entities":[
    {"role":"actor","e":4821,"name":"Urist McGriff","prototype":"MobHuman","player":"a3f2...","g":17,"x":42.1,"y":-17.3},
    {"role":"subject","e":9102,"name":"John Victim","prototype":"MobHuman","player":"b71c...","g":17,"x":41.9,"y":-17.0}
  ]
}
```

`type` is the `LogType` enum name (~120 values), `impact` is `Low|Medium|High`. `msg` is the same
human-readable string admins see today.

`entities[].role` is the name the log's author gave that interpolation hole — `actor`, `subject`, `tool`,
`user`, `target`, and so on. It is **not** a closed set and it is derived from C# variable names at the
call site, so treat it as a display label and a weak grouping hint, never as a schema. Duplicate roles in
one log get `_2`, `_3` suffixes.

Position fields (`g`/`x`/`y`/`c`) on an entity are **absent when that entity is not tracked** — items,
bots, structures, and anything that was never player-controlled. Render those as participants without a
map marker.

A second entry shape appears for the handful of call sites that log coordinates directly:

```json
{"role":"targetCoords","parent":1913774,"x":44.59,"y":-24.39}
```

Distinguish by the presence of `parent`. These are parent-relative, more precise than the sampled cache,
and worth preferring when present.

Chat appears here too, as rows with `"type":"Chat"`, but only as a formatted sentence. For anything that
needs the words as data — speech bubbles, a saylog panel — use `chat.jsonl.gz` (§3.4a) instead, and
filter `type == "Chat"` out of this stream to avoid showing each message twice.

---

## 4. Client data model

Load once, index in memory, then all views are pure lookups.

```
loadBundle(url):
  fetch → DecompressionStream('gzip') → split lines → JSON.parse per line

Indices:
  meta            : parsed meta.json; roster as Map<entityId, RosterEntry>
  positions       : Map<entityId, PositionRow[]>          // per entity, ascending t
  characters      : Map<entityId, CharacterRow[]>         // per entity, ascending t
  health          : Map<entityId, HealthRow[]>            // per entity, ascending t
  control         : Map<entityId, ControlRow[]>           // per entity, ascending t
  navmapChunks    : NavChunkRow[]                         // ascending t, fold on demand
  navmapBeacons   : Map<gridId, BeaconRow[]>              // ascending t
  events          : EventRow[]                            // ascending t
  eventsByEntity  : Map<entityId, EventRow[]>             // built from entities[].e
```

Core accessors:

- `positionAt(entityId, t)` — binary search, last row `t' <= t`. Returns `{g,x,y,c}` or null.
- `characterAt(entityId, t)` — same pattern.
- `healthAt(entityId, t)` — same pattern. Returns `{dmg, state, crit, dead}` or null.
- `controllerAt(entityId, t)` — last row `t' <= t`; returns its `{player, userName}` if that row was an
  `attach`, otherwise null (nobody was driving). Prefer this over `roster[e].player` everywhere a bundle
  attributes an action to an account.
- `navmapAt(gridId, t)` — fold chunk rows with `t' <= t` into `Map<"cx,cy", tiles>`. Cache the fold and
  advance it incrementally while scrubbing forward; only rebuild from scratch on a backward seek.
- `roomAt(gridId, x, y, t)` — nearest beacon. Cheap and high value.

**Performance note.** The dominant cost is `positions`, plausibly a few hundred thousand rows for a long
round. Parse it once into flat typed arrays (`Int32Array` for `t`/`e`/`g`/`c`, `Float32Array` for `x`/`y`)
rather than an array of objects; the per-object overhead is what will hurt, not the parsing. Everything
else is small enough to leave as plain objects.

---

## 5. Views

### 5.1 Map (primary)

Canvas 2D. Per frame at scrub tick `T`, for the selected grid:

1. Fold navmap to `T`, draw floors, then walls, then airlocks. Flat fills, no textures.
2. Draw beacon labels.
3. For each roster entity whose `positionAt(e, T)` has a matching `g`, draw a dot, colour-keyed per
   player, labelled with the character name.
4. Draw a fading trail of that entity's recent positions — **breaking the polyline wherever `g` or `c`
   changes between consecutive samples.**
5. Entities with `c` set get an "inside container" marker rather than a plain dot.

Grid selector in the corner. One grid at a time — mixing frames is incorrect without world transforms,
which v1 does not record.

**Choose the default grid by player presence, not by map size.** Counting position rows per grid picks
where the round actually happened. Ranking by chunk count does not: salvage and expedition grids are
routinely larger than the station *and* first appear tens of minutes in, so a size-ranked default opens on
a grid with no geometry at the round start and renders nothing.

For the same reason, **clamp the opening tick to the chosen grid's first navmap snapshot**, and when the
playhead sits before it, say so — a grid that does not exist yet is not the same as a broken renderer.

### 5.2 Timeline scrubber

Full round on X. Drag to seek, play/pause with speed control. Event markers along the axis, coloured by
`impact`, with `High` always visible and lower impacts filtered by default. Chat bubbles pop at their
tick on the map.

### 5.3 Follow mode

Click an entity → camera locks to it, and a side panel shows `eventsByEntity[e]` scrolling in sync with
the scrubber: everything they did, everything done to them, and everything they said, in one column. Plus
their `characterAt(e, T)` — species, job, access, what is in their hands right now.

This is the view the whole thing exists for.

### 5.4 Swimlanes

One row per roster entity, full round on X, events as marks. No map. Answers "who was busy when" and
"who was near this incident" before you dive into a specific moment. Cheap to build on the same indices.

### 5.5 Incident cards

Given a `Damaged` event whose `msg` contains a state transition to `Critical`/`Dead`, auto-assemble the
±90s window: every event touching either participant, interleaved chat, positions of everyone within N
tiles, and the room name. Generated, not authored.

This is the highest-leverage feature — it is what admins currently assemble by hand from raw log dumps.

---

## 6. Tech

- **Static site.** No backend beyond serving the bundle directories. Everything is client-side.
- **Rendering: Canvas 2D.** This is dots and tiles; WebGL is not warranted. Redraw on scrub, not on rAF,
  unless playing.
- **UI: any light framework** (Svelte/React/vanilla). The rendering does not need one; the chrome benefits.
- **No external asset dependency.** No RSI, no atlas, no sprite pipeline — which is precisely why this is
  a small project and immune to the version-lock that ties engine replays to an exact build.

---

## 7. Compatibility

Read `meta.schema` first and refuse bundles with a major version you do not know, rather than
mis-rendering. Field additions within schema 1 are allowed; readers must ignore unknown keys.

**Whole streams are added within schema 1 too**, so a reader must treat every file except `meta.json` as
optional and degrade rather than fail when one is missing. `health.jsonl.gz` and `control.jsonl.gz` are
both absent from bundles recorded before they existed. Only a change to the *shape* of an existing row
bumps the schema.

Bundles are self-describing and build-independent — no `typeHash` / `componentHash` matching, no
NetSerializer, no content assembly. A bundle from any build reads on any client version that knows its
schema.

---

## 8. Phase 2

Recorded on the server only if these turn out to be needed — each is a genuine addition, not a reader
change:

- **Grid world poses** (`gridpose.jsonl`), sampled ~1Hz, to render multiple grids in one frame (shuttle
  docking against the station).
- **Anchored structure stream** — lockers, tables, windows as `(prototype, g, x, y)`. Navmap does not
  carry them, and "which locker" comes up in exactly the body-disposal cases that matter most.
- **Event-hooked access/weapon changes**, if the 10s character staleness proves too coarse in practice.
  Do not build this speculatively.
- **Adaptive position sampling.** Today the rate is fixed and the only filter is a distance epsilon, so a
  character walking a long straight corridor emits a row every interval even though two endpoints would
  reconstruct the path exactly. Emitting a row only when the actual position deviates from a linear
  prediction (dead reckoning off the last two emitted samples) by more than a threshold would keep full
  fidelity through corners and fights while collapsing straight runs — plausibly another large cut on top
  of the epsilon, since corridors are most of SS14 movement.

  **This is a coupled change, not a server-only one.** Readers currently treat positions as a step
  function (`positionAt` returns the last sample at or before the tick). With adaptive sampling the gaps
  between samples become long and irregular, so a reader *must* interpolate linearly between consecutive
  samples within the same `g`/`c` run, or motion will visibly stutter. Ship both halves together, and
  bump the schema version when doing so.
