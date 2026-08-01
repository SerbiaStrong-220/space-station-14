# Investigation Replay Format

Schema version **1**.

On-disk format of the bundles written by `InvestigationRecorder`.

The producer is authoritative. Where this document and the code disagree, the code is correct.

---

## 1. Bundle layout

```
investigation/round-<roundId>_<yyyy-MM-dd_HH-mm-ss>/
```

`<roundId>` is a decimal integer. The timestamp is UTC, at recording start.

| File | Compression | Content |
|---|---|---|
| `meta.json` | none | Single JSON object (§4.1) |
| `roster.jsonl.gz` | gzip | One row per tracked entity (§4.2) |
| `control.jsonl.gz` | gzip | Player attach/detach transitions (§4.3) |
| `positions.jsonl.gz` | gzip | Entity position vertices (§4.4) |
| `navmap.jsonl.gz` | gzip | Grid tile chunks and beacons (§4.5) |
| `gridpose.jsonl.gz` | gzip | Grid world poses (§4.11) |
| `characters.jsonl.gz` | gzip | Character and loadout snapshots (§4.6) |
| `health.jsonl.gz` | gzip | Damage and mob state samples (§4.7) |
| `objectives.jsonl.gz` | gzip | Objective progress samples (§4.8) |
| `chat.jsonl.gz` | gzip | Chat lines (§4.9) |
| `events.jsonl.gz` | gzip | Admin log stream (§4.10) |

---

## 2. Encoding

| Property | Value |
|---|---|
| Character encoding | UTF-8, no BOM |
| `.jsonl.gz` structure | gzip stream of newline-delimited JSON: one complete object per line |

---

## 3. Common fields

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Absolute game tick. Elapsed seconds = `(t - meta.startTick) / meta.tickRate`. |
| `e` | int | Entity id (`EntityUid`). Join key across all streams. |
| `g` | int \| null | Grid entity id. `null` when the entity is off-grid. |
| `c` | int | Container entity id. Absent when not contained. |
| `x`, `y` | number | Position in tiles. 1 decimal in `positions`, 2 decimals elsewhere. |
| `m` | int | Map id. Names are in `meta.maps` (§4.1). |

**Coordinate frame.**

- `x`/`y` are local to the grid identified by `g`. Comparable only between rows sharing the same `g`.
- Grid-local to map-frame: `world = rotate(local, rot) + (wx, wy)`, using that grid's pose (§4.11).
- When `g` is `null`, `x`/`y` are map-frame coordinates and `m` is present.
- Readers MUST NOT combine grids or entities from different `m` values into one frame.

**Colours.** No field carries a colour. Readers map ids to a palette of their own.

---

## 4. Streams

### 4.1 `meta.json`

Written at round start and again on clean stop.

| Field | Type | Meaning |
|---|---|---|
| `schema` | int | Format version. `1`. |
| `roundId` | int | Round id. |
| `map` | string | Selected map name. Absent when none. |
| `gamemode` | string | `GamePresetPrototype` id. Absent when unresolved. |
| `gamemodeTitle` | string | Localized preset name. |
| `serverName` | string | Value of `admin.server_name`. |
| `startedUtc` | string | ISO-8601 round-trip (`O`), UTC. |
| `startTick` | uint | Tick at recording start. |
| `endTick` | uint | Tick at which this file was last written. |
| `tickRate` | int | Ticks per second. |
| `durationSeconds` | number | Round duration. Absent when the round did not stop cleanly. |
| `languages` | array | Language table. Absent on bundles predating the field. |
| `maps` | array | `{id, name}` for every map a grid pose was written for. Absent on bundles predating the field. |
| `roster` | array | Roster entries, same shape as §4.2. Complete only on clean stop. |

`languages[]`:

| Field | Type | Meaning |
|---|---|---|
| `id` | string | `LanguagePrototype` id. |
| `key` | string | Chat prefix selecting this language, including `%`. |
| `name` | string | Localized language name. |

### 4.2 `roster.jsonl.gz`

```json
{"e":4821,"player":"a3f2c1d0-…","userName":"Kypatop","name":"Urist McGriff","prototype":"MobHuman","firstTick":118420}
```

| Field | Type | Meaning |
|---|---|---|
| `e` | int | Entity id. |
| `player` | string | Account GUID of the first controller. Absent when tracked without a session. |
| `userName` | string | Login of the first controller. Absent as above. |
| `name` | string | Character name at first attachment. Not updated. |
| `prototype` | string | Entity prototype id. |
| `firstTick` | uint | Tick of first attachment. |

Appended once, when an entity is first player-controlled, and not removed. Entities carrying
`GhostComponent` are excluded from this and every other stream. Union with `meta.roster` on `e`.

### 4.3 `control.jsonl.gz`

```json
{"t":118420,"e":4821,"player":"a3f2c1d0-…","userName":"Kypatop","action":"attach"}
{"t":141902,"e":4821,"player":"a3f2c1d0-…","userName":"Kypatop","action":"detach"}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Tick of the transition. |
| `e` | int | Entity id. |
| `player` | string | Account GUID. Absent when no session. |
| `userName` | string | Login. Absent when no session. |
| `action` | string | `attach` or `detach`. |

Controller at tick `T`: the most recent row with `t <= T`; its `player`/`userName` if `action` is
`attach`, otherwise none.

### 4.4 `positions.jsonl.gz`

```json
{"t":118432,"e":4821,"g":17,"x":42.1,"y":-17.3}
{"t":118438,"e":4821,"g":17,"x":42.4,"y":-17.1,"c":9102}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Sample tick. |
| `e` | int | Entity id. |
| `g` | int \| null | Grid id. Always present. |
| `x`, `y` | number | 1 decimal. Non-finite input is written as `0`. |
| `c` | int | Container id. Absent when not contained. |
| `m` | int | Map id. Present only when `g` is `null`. |

Key order within a row is fixed: `t`, `e`, `g`, `x`, `y`, `c`.

Sampled every `investigation.position_interval` (default `0.5`). A row is written when:

1. It is the entity's first sample.
2. `g`, `c`, or `m` changed since the last written row.
3. Movement resumes after a stationary stretch: the held position is re-emitted at the last tick it was
   observed.
4. A held-back sample does not lie within `investigation.position_epsilon` (default `0.15` tiles) of the
   straight line between the last written row and the sample that follows it.

Readers MUST interpolate the position at tick `T` between the last row with `t <= T` and the next row, by
tick. Readers MUST NOT interpolate across a change in `g`, `c`, or `m`.

### 4.5 `navmap.jsonl.gz`

Two row shapes, discriminated by the presence of `tiles` versus `beacons`.

**Chunk row**

```json
{"t":118400,"g":17,"cx":3,"cy":-2,"tiles":[0,240,3855,...]}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Tick. |
| `g` | int | Grid id. |
| `cx`, `cy` | int | Chunk origin, in chunk units. |
| `tiles` | int[64] | 8×8 tile block. Always exactly 64 elements. |

State at tick `T`: fold all chunk rows with `t <= T` into `g → (cx,cy) → tiles`, last write winning.

Tile word layout:

```
CHUNK        = 8
FLOOR_MASK   = 0x00F        bits 0-3
WALL_MASK    = 0x0F0        bits 4-7
AIRLOCK_MASK = 0xF00        bits 8-11

index -> chunk-relative position, COLUMN-major:
    x = index / CHUNK           integer division
    y = index % CHUNK

grid-local tile coordinates:
    tileX = cx * CHUNK + x
    tileY = cy * CHUNK + y

presence:
    hasFloor   = (tiles[index] & FLOOR_MASK)   != 0
    hasWall    = (tiles[index] & WALL_MASK)    != 0
    hasAirlock = (tiles[index] & AIRLOCK_MASK) != 0
```

**Beacon row**

```json
{"t":118400,"g":17,"beacons":[{"name":"Medbay","x":42.0,"y":-17.0}]}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Tick. |
| `g` | int | Grid id. |
| `beacons` | array | Complete beacon set for `g` at `t`. |

`beacons[]`: `name` (string), `x`/`y` (number, grid-local).

Each row replaces the prior beacon set for that grid.

### 4.6 `characters.jsonl.gz`

```json
{"t":118400,"e":4821,"name":"Urist McGriff","species":"Human","gender":"Male","age":34,
 "job":"SecurityOfficer","department":"Security",
 "antag":true,"roles":[{"id":"Traitor","name":"Traitor"}],
 "access":["Armory","Security"],
 "worn":{"outerClothing":"ClothingOuterHardsuitSecurity","id":"SecurityPDA"},
 "hands":["WeaponPistolMk58"],
 "carried":["Handcuffs","Flashlight"]}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Tick of the snapshot. |
| `e` | int | Entity id. |
| `name` | string | Displayed name at `t`. |
| `species`, `gender` | string | Absent for non-humanoids. |
| `age` | int | Absent for non-humanoids. |
| `job` | string | Job prototype id. Absent when the entity has no job. |
| `department` | string | Department prototype id. Absent with `job`. |
| `antag` | bool | `true`. Present only on antagonists. |
| `roles` | array | Present only on antagonists. |
| `access` | string[] | Sorted ordinal. |
| `worn` | object | Inventory slot id → entity prototype id. |
| `hands` | string[] | Held entity prototype ids. |
| `carried` | string[] | Storage contents to `investigation.storage_depth` (default `2`). |

`roles[]`: `id` (antag prototype id), `name` (localized string).

Written when the loadout fingerprint changes, evaluated every `investigation.character_interval`
(default `10`). State at `T`: last row with `t <= T`. `antag !== true` identifies a non-antag row.

### 4.7 `health.jsonl.gz`

```json
{"t":118432,"e":4821,"dmg":37.5,"state":"Alive","crit":100.0,"dead":200.0}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Sample tick. |
| `e` | int | Entity id. |
| `dmg` | number | Total damage, 2 decimals. |
| `state` | string | `MobState` name: `Alive`, `Critical`, `Dead`, `Invalid`. |
| `crit` | number | Absent when undefined for the entity. |
| `dead` | number | Absent when undefined for the entity. |

Sampled on `investigation.position_interval`. Written when `dmg` or `state` differs from the previous row
for that entity. State at `T`: last row with `t <= T`.

### 4.8 `objectives.jsonl.gz`

```json
{"t":118500,"o":9931,"e":4821,"proto":"KillPersonObjective","title":"Kill Ian Doe","progress":0.0,"done":false}
{"t":186200,"o":9931,"e":4821,"proto":"KillPersonObjective","title":"Kill Ian Doe","progress":1.0,"done":true}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Sample tick. |
| `o` | int | Objective entity id. Grouping key. |
| `e` | int | Entity whose mind holds the objective. |
| `proto` | string | Objective entity prototype id. |
| `title` | string | Localized text. |
| `desc` | string | Absent when the objective has none. |
| `progress` | number | `0.0`–`1.0`, 2 decimals. |
| `done` | bool | `progress >= 0.999`. |

Sampled on `investigation.character_interval`. Written when rounded `progress` or `done` changes.
Completion tick: the `t` of the first row with `done == true`.

### 4.9 `chat.jsonl.gz`

```json
{"t":118432,"e":4821,"ch":"Say","name":"Urist McGriff","msg":"привет, мир","lang":"Galactic","g":17,"x":42.1,"y":-17.3}
{"t":118440,"e":4821,"ch":"Radio","name":"Urist McGriff","msg":"code red","lang":"Galactic","rc":"Security"}
{"t":118450,"ch":"OOC","name":"Kypatop","msg":"ooc line"}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Tick. |
| `e` | int | Speaking entity. Absent for `OOC`, `AdminChat`. |
| `ch` | string | `Say`, `Whisper`, `Radio`, `Emote`, `LOOC`, `Dead`, `OOC`, `AdminChat`. |
| `name` | string | Displayed name at `t`. |
| `msg` | string | Untransformed text. `%key` language prefixes are retained. |
| `lang` | string | Speaker's selected language id. Absent on `Emote`, `LOOC`, `OOC`, and on bundles predating the field. |
| `langs` | string[] | Present only when the line contains two or more languages. |
| `rc` | string | Present only when `ch == "Radio"`. |
| `g`, `x`, `y`, `c` | — | Speaker position at `t`. Absent when no position could be resolved. |

Every row also appears in §4.10 with `type == "Chat"`, without a text field.

### 4.10 `events.jsonl.gz`

```json
{"t":118432,"utc":"2026-07-29T14:32:07.1230000Z","type":"MeleeHit","impact":"High",
 "msg":"Urist McGriff (4821/n4821, MobHuman, Kypatop) melee attacked ...",
 "entities":[
   {"role":"actor","e":4821,"name":"Urist McGriff","prototype":"MobHuman","player":"a3f2...","g":17,"x":42.1,"y":-17.3},
   {"role":"subject","e":9102,"name":"John Victim","prototype":"MobHuman","player":"b71c...","g":17,"x":41.9,"y":-17.0}
 ]}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Tick. |
| `utc` | string | ISO-8601 round-trip (`O`), UTC. |
| `type` | string | `LogType` enum name. |
| `impact` | string | `Low`, `Medium`, `High`. |
| `msg` | string | Formatted log message. |
| `entities` | array | Resolved interpolation holes. |

`entities[]`, entity shape:

| Field | Type | Meaning |
|---|---|---|
| `role` | string | Duplicates within one log are suffixed `_2`, `_3`. |
| `e` | int | Entity id. |
| `name` | string | Entity name at log time. |
| `prototype` | string | Entity prototype id. |
| `player` | string | Account GUID, when the entity had a session. |
| `g`, `x`, `y`, `c` | — | Position. Absent when the entity is not tracked. |

`entities[]`, coordinate shape, discriminated by the presence of `parent`:

```json
{"role":"targetCoords","parent":1913774,"x":44.59,"y":-24.39}
```

| Field | Type | Meaning |
|---|---|---|
| `role` | string | `"targetCoords"`. |
| `parent` | int | Entity the coordinates are relative to. |
| `x`, `y` | number | Position relative to `parent`. |

### 4.11 `gridpose.jsonl.gz`

```json
{"t":118400,"g":17,"m":3,"wx":412.5,"wy":-88.0,"rot":0.0}
```

| Field | Type | Meaning |
|---|---|---|
| `t` | uint | Sample tick. |
| `g` | int | Grid entity id. |
| `m` | int | Map id. Names are in `meta.maps` (§4.1). |
| `wx`, `wy` | number | Grid origin in the map frame, 2 decimals. |
| `rot` | number | Grid rotation in radians, 4 decimals. |

Sampled on `investigation.navmap_interval` (default `1`). Written when the pose moves more than 0.05
tiles or radians, or `m` changes.
A grid may have a pose with no corresponding rows in `navmap` (§4.5).

---

## 5. Reconstruction

| Query | Method |
|---|---|
| Position of `e` at `T` | Interpolate between the last `positions` row with `t <= T` and the next, by tick. Do not cross a `g`/`c`/`m` change. |
| Character/loadout of `e` at `T` | Last `characters` row with `t <= T`. |
| Health of `e` at `T` | Last `health` row with `t <= T`. |
| Controller of `e` at `T` | Last `control` row with `t <= T`; its account if `action == "attach"`, else none. |
| Antagonists at `T` | Entities whose `characters` row at `T` has `antag == true`. |
| Objective outcome | Group `objectives` by `o`; completion tick is the first row with `done == true`. |
| Grid geometry at `T` | Fold `navmap` chunk rows with `t <= T`, keyed `(g, cx, cy)`, last write winning. |
| Grid pose at `T` | Last `gridpose` row for that `g` with `t <= T`. |
| Beacon set at `T` | Last `navmap` beacon row for that `g` with `t <= T`. |
| Room containing a position | Nearest beacon on the same `g`. |

---

## 6. Versioning

Readers MUST read `meta.schema` first and reject a major version they do not implement.

Within schema `1`:

- Fields MAY be added to any object.
- Streams MAY be added. Every file other than `meta.json` is OPTIONAL.

A change to the shape or meaning of an existing field increments `schema`.
