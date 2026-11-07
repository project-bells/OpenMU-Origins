# OpenMU Origins - Work Handoff

## Automatic mandatory configuration updates

- [x] Apply pending mandatory configuration update plugins automatically before the All-In-One server loads game configuration.
- [x] Include pending transitive dependencies without enabling unrelated optional/custom gameplay updates.
- [x] Fail startup when a required update fails, preventing a partially updated server from accepting players.
- [x] Preserve runtime plugin defaults: required plugins remain active, while security-sensitive GM/reset/custom plugins stay opt-in.

Last updated: 2026-11-05
Target: MU Online Season 6 Episode 3, ENG protocol 1.04d  
Branch: `master`  
Remote: `origin` (`https://github.com/project-bells/OpenMU-Origins.git`)

## Current status

The repository is **not yet certified production-ready**. The detailed audit is
maintained in `docs/SEASON6_PRODUCTION_READINESS_AUDIT.md`; this file is the
short operational handoff for continuing the work on another machine.

Latest completed gameplay commit at the time of this handoff:

- `9f2a830a7` - `test(mu-helper): cover concurrent disconnect cleanup`

Recent completed work:

- `9f2a830a7` - Extended the concurrent MU Helper shutdown test through the
  player's real character-disconnect path while explicit stop and disposal race
  it, proving that all three paths complete with one stop notification.
- `c0ac42dbc` - Serialized MU Helper start/stop/dispose lifecycle transitions,
  rejected malformed settings packets and blobs outside the exact 257-byte
  payload, and added integration coverage for save/echo, reconnect restoration,
  all decoded runtime settings, level gates, initial/recurring Zen charges,
  insufficient-Zen shutdown, and concurrent shutdown calls.
- `238569f27` - Added exact Season 6 Kanturu Maya/Nightmare wave, boss,
  skill, health, drop-count, and respawn coverage; expanded Raklion coverage to
  every spider-egg and Coolutin summon coordinate; and locked all nine Golden
  Invasion tiers to their duration, pre-start delay, announcements, hourly
  timetable, quantities, map eligibility, and random-coordinate strategy.
- `f27fcd9ee` - Moved fixed Chaos Castle Bless/Soul monster drops into editable
  per-map Season 6 configuration, added a mandatory idempotent update for
  existing databases, and covered distinct-kill exactly-once generation.
- `7602a67b0` - Made Chaos Castle explosion chance and knockback deterministic
  under test without changing runtime behavior, covered push direction and map
  boundaries, and verified all Chaos Castle 1-7 jewel/Ancient Item winner reward
  groups, chances, success flags, and quantities.
- `9fafe62ad` - Added fresh Season 6 configuration coverage for Chaos Castle
  1-7 safezone removal, kill thresholds, and all three exact floor-collapse
  stages, including terrain attributes and client-update behavior.
- `f0708e6bc` - Added deterministic Chaos Castle PvP damage coverage for
  shield-era and classic PvP, including the Dark Raven exception.
- `276b5bb9d` - Added Chaos Castle entry coverage proving that the entrant is
  removed alone from its existing party, the remaining party stays intact, and
  regrouping from inside the event is rejected.
- `d70e90125` - Added fresh Season 6 configuration coverage for Chaos Castle
  1-7 player capacity, randomized entrance domain, 99-monster population,
  per-level monster pairs, manual spawn triggers, and unique arena positions.
- `ebaed4c5e` - Added Blood Castle and Devil Square lifecycle coverage proving
  that the last player leaving a running event cancels it, clears registration,
  and reports an empty finisher set.
- `7b45641fc` - Added fresh Season 6 configuration coverage for Blood Castle
  1-8 score, experience, Zen, and Jewel of Chaos rewards, plus Devil Square
  1-7 rank-based experience and Zen rewards.
- `ce6025736` - Serialized Blood Castle Archangel quest hand-ins so only the
  winning owner consumes a quest item, with coverage for non-owner attempts and
  duplicate claims retaining the losing claimant's item.
- `75e7b8714` - Added fresh Season 6 configuration coverage for the Blood
  Castle 1-8 gate, statue, and Archangel weapon quest sequence, including
  kill scaling, terrain changes, health scaling, and guaranteed quest drop.
- `30923029f` - Added fresh Season 6 configuration coverage for Blood Castle
  1-8 monster definitions, spawn counts, quantities, triggers, map references,
  and coordinates, plus Devil Square 1-7 monster sets and wave timing.
- `c3a11316d` - Added Blood Castle and Devil Square integration coverage for
  preserving party membership and reporting exact level, ticket, and capacity
  rejection results without consuming invitations.
- `51ee64e64` - Added Blood Castle and Devil Square entry integration tests
  proving that successful registration consumes one invitation use, while
  level rejection, wrong ticket level, and full-event rejection consume none.
- `724972a1a` - Added fresh Season 6 configuration coverage for all Blood
  Castle 1-8 and Devil Square 1-7 normal/special/master entrance brackets,
  event maps, invitation levels, party policy, and shared-map policy.
- `049df5424` - Added deterministic Chaos Castle full-cycle tests for early
  last-survivor victory, ranked timeout victory, no-survivor completion, reset,
  concurrent terminal arbitration, and exactly-once reward/ranking completion.
- `b76e44d9f` - Added deterministic Devil Square full-cycle tests for start,
  survival timeout with finishers, empty-event completion, reset, concurrent
  terminal arbitration, and exactly-once reward/ranking completion.
- `6162a7189` - Added deterministic Blood Castle full-cycle tests for ordered
  gate/statue/quest milestones, timeout at every milestone, reset, concurrent
  terminal arbitration, and exactly-once successful reward completion.
- `e0c721f76` - Added deterministic Raklion/Selupan full-cycle tests for
  success, close timeout, empty-battle failure, reset, concurrent terminal
  precedence, and exactly-once successful reward completion.
- `225696efa` - Added deterministic Imperial Guardian full-zone-cycle tests for
  ordered zone execution, success, first/middle-zone timeout, reset, and
  exactly-once successful reward completion.
- `9f331b7c0` - Added deterministic Doppelganger outcome-cycle tests for
  time-limit success, goal-threshold failure, concurrent terminal events, reset,
  and exactly-once final reward completion.
- `dc3ec9fff` - Added deterministic Kanturu Maya-to-Nightmare full-cycle tests
  for success, Maya timeout, Nightmare timeout, reset to the next cycle, ordered
  phase execution, and exactly-once successful reward completion.
- `8ac41073f` - Fixed a Mu Helper self-deadlock when the helper loop stops itself
  after the character can no longer pay the Zen charge.
- `e5b8970bc` - Event entry now rejects characters whose duel is requested,
  accepted, or running.
- `ca947394c` - Disabled entry to the incomplete Illusion Temple runtime.
- Illusion Temple ticket crafting and cash-shop exposure are disabled for fresh
  and existing databases until the event runtime is implemented.
- Unsupported mini-game reward values are logged and skipped instead of
  terminating reward processing.
- Season 6 master-skill data, Rage Fighter behavior, ancient sets, Chaos Machine
  protections, Harmony, 380 mix, socket validation, and event schedules received
  targeted fixes and tests. See the audit report and commit history for details.

Last verification performed:

- `dotnet build src/MUnique.OpenMU.sln -p:ci=true`: succeeded with 0 errors
  (existing analyzer warnings remain).
- Targeted Mu Helper and offline behavior run: 149 passed, 0 failed.
- Full `MUnique.OpenMU.Tests` run: 2,036 passed, 0 failed.
- Full `MUnique.OpenMU.Network.Packets.Tests` run: 645 passed, 0 failed.
- Full `MUnique.OpenMU.Persistence.Initialization.Tests` run: 676 passed,
  7 skipped, 0 failed.
- The solution still emits existing analyzer warnings. Do not treat the build as
  warning-clean.

## Start here on another machine

```powershell
git clone https://github.com/project-bells/OpenMU-Origins.git
Set-Location OpenMU-Origins
git checkout master
git pull --ff-only origin master
git status --short --branch
dotnet --info
dotnet build src/MUnique.OpenMU.sln -p:ci=true
```

Expected starting point: `master` contains commit `e5b8970bc` or a later commit,
and the working tree is clean. Read these files before modifying code:

1. `AGENTS.md`
2. `docs/CODING_RULES.md`
3. `docs/SEASON6_PRODUCTION_READINESS_AUDIT.md`
4. This `work.md`

For each change, keep one concern per commit, add or update automated tests,
build after source changes, and use conventional commit subjects.

## Recommended next development rounds

### Round 1 - Event state-machine tests (completed)

Add deterministic full-cycle integration tests before changing more event logic.
Start with the event implementations which already exist:

1. Kanturu (Maya and Nightmare) - full-cycle success/timeout/reset and
   exactly-once completion covered by `dc3ec9fff`
2. Doppelganger - success/failure/reset, concurrent terminal events, and
   exactly-once final reward completion covered by `9f331b7c0`
3. Imperial Guardian - ordered zones, success/timeout/reset, and exactly-once
   successful reward completion covered by `225696efa`
4. Raklion (Selupan) - success/timeout/reset, concurrent terminal precedence,
   and exactly-once successful reward completion covered by `e0c721f76`
5. Blood Castle - ordered gate/statue/quest milestones, timeout/reset,
   concurrent terminal arbitration, and exactly-once successful reward
   completion covered by `6162a7189`
6. Devil Square - start, survival timeout/empty completion, reset, concurrent
   terminal arbitration, and exactly-once reward/ranking completion covered by
   `b76e44d9f`
7. Chaos Castle - early last-survivor victory, ranked timeout victory,
   no-survivor completion, reset, concurrent terminal arbitration, and
   exactly-once reward/ranking completion covered by `049df5424`

Each test must cover:

- scheduled opening and entrance closing;
- level/master-level and ticket eligibility;
- monster or wave spawn order;
- success and timeout paths;
- player death, disconnect, and rejoin;
- reward delivery exactly once;
- cleanup/map disposal;
- reset to the next scheduled start without a stalled task or deadlock.

Relevant locations:

- `src/GameLogic/MiniGames`
- `src/GameLogic/PlugIns/PeriodicTasks`
- `src/GameLogic/PlayerActions/MiniGames/EnterMiniGameAction.cs`
- `src/PlugIns`
- `tests/MUnique.OpenMU.Tests`

Do not claim parity merely because a state machine compiles. Compare observable
behavior with an original ENG 1.04d client packet trace and in-game observation.

### Round 2 - Blood Castle and Devil Square parity

Audit all configured levels (Blood Castle 1-8, Devil Square 1-7):

- normal-level and master-level brackets - fresh configuration coverage added
  by `724972a1a`;
- invitation/ticket level covered by `724972a1a`; consumption timing on
  success and rejected entry covered by `51ee64e64`;
- party preservation and level/ticket/capacity failure results covered by
  `c3a11316d`;
- monster definitions, counts, spawn positions, and wave timing covered by
  `30923029f`;
- Blood Castle gate/statue/Archangel weapon quest sequence covered by
  `75e7b8714`;
- quest hand-in ownership and duplicate-claim prevention covered by
  `ce6025736`;
- score, experience, Zen, and item rewards covered by `7b45641fc`;
- timeout covered by `6162a7189` and `b76e44d9f`; empty-event lifecycle cleanup
  covered by `ebaed4c5e`.

Any persisted configuration correction requires both:

1. a correction in `src/Persistence/Initialization`, and
2. an idempotent `IConfigurationUpdatePlugIn` for existing databases.

Test fresh initialization, an upgrade from the previous state, and a second
idempotent application.

### Round 3 - Chaos Castle parity and drop configuration

Audit Chaos Castle 1-7:

- player/monster population and random placement covered by `d70e90125`;
- client-side disguise map identities covered by `d70e90125`; party removal
  and in-event regroup prevention covered by `276b5bb9d`;
- PvP damage rules, including classic PvP and Dark Raven, covered by
  `f0708e6bc`;
- floor-collapse stages and safe tiles covered by `9fafe62ad`;
- explosion traps and knockback/death handling covered by `7602a67b0`;
- winner selection when the last deaths occur close together covered by
  `049df5424`;
- Ancient Item reward generation covered by `7602a67b0`, with exactly-once
  winner delivery covered by `049df5424`.

The fixed Bless/Soul selection in `ChaosCastleDropGenerator` was moved into
Season 6 configuration by `f27fcd9ee`, including fresh initialization, a
mandatory idempotent update for existing databases, and deterministic drop
tests. Round 3 is complete.

### Round 4 - Bosses and invasions

Completed in `238569f27`, together with the Round 1 full-cycle tests in
`dc3ec9fff` and `e0c721f76`:

- Kanturu Maya and Nightmare exact wave coordinates, skills, health, maximum
  drops, and non-respawning event definitions;
- Raklion Selupan, every spider-egg coordinate, every summoned Coolutin
  coordinate, and all four Selupan skill multipliers;
- all nine Golden Invasion tiers, including timing, announcements, quantities,
  eligible maps, and random-coordinate strategy.

The shared invasion lifecycle already covers start/end announcements and cleanup;
the periodic-task tests cover restart isolation, forced starts, duration guards,
and subsequent scheduled runs. Generic monster-drop tests cover killer/party
ownership independently of the spawning event.

Medusa remains blocked and Round 4 must not be claimed fully complete yet. The
repository contains neither a Medusa monster definition nor an event plug-in.
Webzen's Season 5.4 notice confirms only that Medusa appears at random times in
the Swamp of Peace; it does not provide the ENG 1.04d monster ID, exact spawn
coordinates, skills, timing distribution, or drop table. Obtain an original
ENG 1.04d client trace plus in-game observations before implementing and testing
those values; do not copy them from third-party server sources.

### Round 5 - Mu Helper protocol and economy

The automatable server-side scope is complete in `c0ac42dbc` and `9f2a830a7`:

- settings save/echo preserves the exact 257-byte payload and reconnect restores
  the decoded settings;
- short and oversized packets/blobs are rejected before parsing or persistence;
- minimum and maximum levels are inclusive, with both rejection boundaries
  covered;
- the initial Zen charge and the first recurring interval are covered;
- insufficient Zen stops once, sends the stop notification, clears the active
  attribute, and does not self-deadlock;
- concurrent stop/disconnect-style cleanup/dispose calls share one serialized
  shutdown and one client notification;
- the settings fixture covers attack and conditional skills, buffs, heal and
  potion thresholds, repair, movement/regrouping, pet mode, and every pickup
  filter; the offline handler suite covers their runtime behavior.

The generated packet definitions remain the source of truth and the full packet
structure suite passes. No generated packet C# or packet documentation was
edited.

Round 5 protocol certification still requires an original ENG 1.04d client
capture. The current 257-byte field layout is now locked by integration tests,
but a capture is required to independently prove every bit offset and client
reaction instead of treating the existing implementation as its own oracle.

Relevant locations:

- `src/GameLogic/MuHelper`
- `src/GameLogic/PlayerActions/MuHelper`
- `src/GameServer/MessageHandler`
- `src/GameServer/RemoteView/MuHelper`
- `src/Network/Packets`
- `tests/MUnique.OpenMU.Tests/Offline`

Packet structures are generated. Modify packet XML sources, never generated
packet C# or generated packet documentation directly.

### Round 6 - Character classes and master skills

The automatable data and lifecycle audit is complete in `047f23f0e`:

- all seven class progressions (DK, DW, Elf, MG, DL, Summoner, and Rage
  Fighter) have one creatable base class, the configured evolution chain, one
  terminal master class, and master-point progression data;
- every configured normal and master skill has a unique identifier and valid
  damage, hit-count, range, class qualification, requirement, and resource-cost
  references;
- every master skill has valid allocation bounds, formulas, root/rank data,
  class qualification, prerequisite references, and replacement links;
- the audit covers both master-skill prerequisites and normal-skill
  prerequisites, matching the two paths enforced by `AddMasterPointAction`;
- Rage Fighter and Fist Master combat relationships consume configured total
  strength, agility, vitality, and energy attributes rather than hard-coded
  GameLogic class branches;
- repeated character initialization (the reset/reconnect path) preserves the
  learned master-skill instance and its level.

The full GameLogic suite passes with 2,037 tests. The Persistence
Initialization run passes 676 other tests with seven existing skips, and the
final four-test Round 6 audit passes after its last rebuild. The full
solution builds with zero errors (existing repository warnings remain).

Round 6 client-visible certification still requires an original ENG 1.04d
capture and in-game observations. The server-side audit locks the configured
values and graph consistency, but it cannot independently prove client-rendered
cooldowns, buff text, animation timing, or displayed Rage Fighter formula
rounding without treating the current configuration as its own oracle.

### Round 7 - Guild, Gens, duel, and Castle Siege

Completed in `11109a549`, `049352e07`, `efda25310`, `ae3c9f87a`,
`b33b153bc`, `ffb92b299`, and `33e517512`.

The multiplayer integration coverage now exercises the Castle Siege
registration-to-ownership flow, guild selection and participation, crown and
switch contention, capture/result persistence, NPC upgrade and destruction
state, and recovery after each scheduled-state restart. It also covers remote
view cleanup after disconnects and pending rewards for online, offline,
reconnected, and next-login players without duplicate delivery. Fifty
concurrent store payments verify that player money and castle tribute are both
updated exactly once under the shared execution lock.

Guild scenarios cover alliance and hostility propagation across two game-server
endpoints. Gens scenarios cover relationship restrictions, ranking, monthly
rewards, and single-claim behavior. Duel lifecycle tests cover immediate
re-entry after request cleanup, spectator removal and re-entry, and delayed
cleanup of an old duel without releasing a newly allocated replacement room.
The duel cleanup implementation now clears player/spectator room ownership and
only returns the room instance which still owns its manager slot.

The final GameLogic build passes with zero errors (existing repository warnings
remain). The focused Round 7 run passes all four duel/tax tests, and the full
GameLogic suite passes all 2,041 tests with no skips or failures.

These are deterministic in-process integration tests, including two simulated
game-server endpoints. A real multi-process 24-hour concurrency/soak run remains
a production blocker below; Round 7 does not claim that external certification.

## Authoritative WebShop inventory API

Implemented on 2026-11-07:

- CashShop-role-only catalog discovery at `GET /api/cash-shop/catalog`;
- exactly-once item delivery at
  `POST /api/accounts/{loginName}/cash-shop/item-grants`;
- server-side resolution of package/product sequences from the active OpenMU
  configuration, with no accepted item hex, serial, price, or option bytes;
- one transaction for the unique order audit record and every resulting cash
  shop storage item, including bundle products;
- reference replay returns the original result, while reference reuse for
  another account or selection is rejected;
- EF migration and regenerated BasicModel/EF/compiled models;
- focused tests for success, retry, cross-account/product conflict, invalid
  selection, bundle atomicity, and catalog discovery.

`OpenMU-WebShop` now uses a persistent pending-order ledger, debits Credits once,
and submits only the mapped package/price sequence to this API. Ambiguous
timeouts remain pending and reuse the same browser-persisted reference; definitive
safe failures refund exactly once, while reference conflicts require manual
review. Admin mappings are checked against this catalog and persisted. Unmapped
items, browser-selected item options, WCoin item purchases, group purchases, and
refunds remain fail-closed. Enabling the old item-hex path would reintroduce the
forgery and duplication risks this API removes.

## In-game shop and VIP room 3

Implemented on 2026-11-07:

- the Season 6 Cash Shop configuration update is mandatory, enabling the stock
  client X-button catalog for existing Origins databases;
- fresh Season 6 configuration creates `SV3-VIP` (server ID 2) at EXP 130x;
- `Account.VipUntil` is persisted through the `AddVipMembership` migration;
- non-VIP and expired accounts are rejected from room 3 before login completes,
  while active VIP and game-master accounts are accepted;
- CashShop-role API grants 30-360 VIP days exactly once by immutable reference;
- WebShop reserves Credits once, retries ambiguous results, refunds definitive
  failures once, and mirrors only the expiry returned by OpenMU.

## Known production blockers

The following must be completed before calling the server production-ready:

- Original ENG 1.04d client compatibility test with packet captures.
- Upgrade rehearsal using a backup of a real pre-update production database.
- Verification that configuration updates are idempotent on a second run.
- At least a 24-hour multi-server concurrency/soak test.
- Exploit/economy testing for duplication, crafting disconnects, repeated reward
  claims, malformed packets, overflow/negative currency, and Castle Siege races.
- Backup/restore, monitoring, alerting, secrets, deployment, and rollback runbook.
- Illusion Temple remains incomplete and intentionally unavailable.

## Additional P1/P2 backlog

- Complete the Season 6 socket bonus sequence matrix for Seed Sphere levels 1-5.
- Perform a machine-readable comparison of every Season 6 item definition.
- Add excellent-option probability and uniqueness tests for 1-6 options.
- Verify Life/Mana recovery and steal effects during real multi-hit attacks.
- Review the missing Archer and Spearman merchant entries in
  `src/Persistence/Initialization/VersionSeasonSix/MerchantStores.cs`.
- Review the per-level wing requirement note in
  `src/Persistence/Initialization/VersionSeasonSix/Items/Wings.cs`.
- Add packet-boundary fuzz tests and invalid persisted-enum tests.
- Add performance budgets for combat, item generation, event waves, navigation,
  and persistence saves.
- Explain or remove intentionally skipped initialization tests.

## Test commands

Build once after changes, then use `--no-build` while the binaries remain fresh:

```powershell
dotnet build src/MUnique.OpenMU.sln -p:ci=true
dotnet test tests/MUnique.OpenMU.Tests/MUnique.OpenMU.Tests.csproj --no-build
dotnet test tests/MUnique.OpenMU.Persistence.Initialization.Tests/MUnique.OpenMU.Persistence.Initialization.Tests.csproj --no-build
dotnet test tests/MUnique.OpenMU.Network.Tests/MUnique.OpenMU.Network.Tests.csproj --no-build
dotnet test tests/MUnique.OpenMU.Network.Packets.Tests/MUnique.OpenMU.Network.Packets.Tests.csproj --no-build
```

For a focused event/helper pass:

```powershell
dotnet test tests/MUnique.OpenMU.Tests/MUnique.OpenMU.Tests.csproj --no-build --filter "FullyQualifiedName~MiniGame|FullyQualifiedName~ZenConsumptionHandlerTests|FullyQualifiedName~MuHelper"
```

Build again after any source, project, package, generator input, or packet XML
change. Do not run `--no-build` against stale binaries.

## Database initialization and upgrades

When item, monster, skill, map, drop, or event configuration changes:

- Fresh database: update the Season 6 initializer and recreate only a disposable
  development/test database through the normal initialization workflow.
- Existing database: add or version an idempotent configuration update plugin;
  do not recreate the production database.
- Back up the database before applying updates.
- Apply all pending mandatory updates from the Admin Panel/configuration update
  workflow, restart services, and scan again. The second scan must not offer an
  already-installed version.
- Add tests for fresh initialization, upgrade from the prior state, and repeated
  application.

If a DataModel property changes, also assess whether an Entity Framework
migration is required. Follow
`src/Persistence/EntityFramework/Migrations/Cheatsheet.md`.

## Per-round Git checklist

```powershell
git pull --ff-only origin master
git status --short
git diff --check
dotnet build src/MUnique.OpenMU.sln -p:ci=true
# Run relevant tests here.
git add -- <focused-files>
git commit -m "type(scope): concise imperative subject"
git push origin master
git status --short --branch
```

Before pushing, confirm that local development configuration, secrets, packet
captures containing credentials, database backups, logs, and generated scratch
files are not staged.

## Definition of done

Production readiness requires all P0 validation, no enabled feature with an
unimplemented runtime path, repeatable full-cycle tests for supported events,
and a release candidate which survives the upgrade and soak rehearsals without
item/reward duplication, deadlocks, unbounded memory growth, persistence
conflicts, or original-client protocol incompatibility.
