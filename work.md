# OpenMU Origins - Work Handoff

Last updated: 2026-11-04  
Target: MU Online Season 6 Episode 3, ENG protocol 1.04d  
Branch: `master`  
Remote: `origin` (`https://github.com/project-bells/OpenMU-Origins.git`)

## Current status

The repository is **not yet certified production-ready**. The detailed audit is
maintained in `docs/SEASON6_PRODUCTION_READINESS_AUDIT.md`; this file is the
short operational handoff for continuing the work on another machine.

Latest completed gameplay commit at the time of this handoff:

- `724972a1a` - `test(events): cover Blood Castle and Devil Square entry data`

Recent completed work:

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
  and 0 warnings in the final incremental build.
- Targeted Blood Castle/Devil Square initialization run: 2 passed, 0 failed.
- Full `MUnique.OpenMU.Persistence.Initialization.Tests` run: 664 passed,
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
- invitation/ticket level covered by `724972a1a`; consumption timing remains;
- party behavior and entrance failure results;
- monster definitions, counts, spawn positions, and wave timing;
- Blood Castle gate/statue/Archangel weapon quest sequence;
- quest hand-in ownership and duplicate-claim prevention;
- score, experience, Zen, and item rewards;
- timeout and empty-event cleanup.

Any persisted configuration correction requires both:

1. a correction in `src/Persistence/Initialization`, and
2. an idempotent `IConfigurationUpdatePlugIn` for existing databases.

Test fresh initialization, an upgrade from the previous state, and a second
idempotent application.

### Round 3 - Chaos Castle parity and drop configuration

Audit Chaos Castle 1-7:

- player/monster population and random placement;
- disguise and party removal behavior;
- PvP damage rules;
- floor-collapse stages and safe tiles;
- explosion traps and knockback/death handling;
- winner selection when the last deaths occur close together;
- Ancient Item reward generation and exactly-once delivery.

`src/GameLogic/MiniGames/ChaosCastleDropGenerator.cs` still has fixed selection
behavior which should be moved into Season 6 configuration. Add initialization
data, an update plugin for existing databases, and deterministic drop tests.

### Round 4 - Bosses and invasions

Add end-to-end parity tests for:

- Kanturu Maya and Nightmare;
- Raklion Selupan;
- Medusa;
- all Golden Invasion tiers.

Validate coordinates, timing, skills, announcements, drop ownership, cleanup,
restart recovery, and the next scheduled run.

### Round 5 - Mu Helper protocol and economy

The server-side helper exists, and the known self-deadlock is fixed. Remaining
work needs an original-client trace and integration coverage for:

- settings save/load and exact packet round-trip;
- minimum/maximum level checks;
- initial Zen charge and recurring charge interval;
- insufficient-Zen stop notification and absence of deadlock;
- reconnect restoration;
- skill selection, buffs, auto-potion, repair, movement, and pickup filters;
- malformed or truncated helper settings;
- concurrent stop/disconnect/dispose calls.

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

Perform a data-driven audit for all seven classes: DK, DW, Elf, MG, DL,
Summoner, and Rage Fighter. Check:

- base/evolution class requirements and point progression;
- every normal and master skill requirement;
- master point allocation and prerequisite graph;
- damage, multi-hit count, cooldown, range, buffs, and resource cost;
- Rage Fighter formulas and client-visible behavior in particular;
- reset/reconnect persistence of learned master skills.

Prefer configured attribute relationships and skill data over hard-coded class
branches in GameLogic.

### Round 7 - Guild, Gens, duel, and Castle Siege

Create multiplayer and multi-server integration scenarios. Castle Siege needs a
complete registration-to-ownership cycle, crown/switch races, NPC persistence,
tax concurrency, reward recovery, disconnects, and service restarts in every
schedule state. Also validate guild alliances/hostility, Gens rank and rewards,
and duel cleanup/re-entry behavior.

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
