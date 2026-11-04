# Season 6 Episode 3 Production Readiness Audit

Status date: 2026-11-04  
Target: ENG protocol 1.04d, Season 6 Episode 3  
Overall status: **not yet certified production-ready**

This document tracks the repository audit requested for the Season 6 Episode 3
gameplay configuration and runtime. A checked item means that the implementation
and its automated tests were reviewed in this audit. It does not replace a
multi-client soak test against a production database and the original 1.04d
client.

## Current verification baseline

- `MUnique.OpenMU.Tests`: 1,964 passed, 0 failed.
- `MUnique.OpenMU.Persistence.Initialization.Tests`: 660 passed, 0 failed,
  7 intentionally skipped.
- Season 6 initialization builds successfully with `-p:ci=true`.
- The working configuration contains update plugins for existing databases where
  initialization data was changed.
- Latest audited commit on `master`: `9bcf3b292`.

## Completed in this audit

### Items, excellent options, and ancient sets

- [x] Corrected the excellent damage-rate option and the level-divided-by-20
  excellent damage option.
- [x] Corrected Kantata's excellent damage chance and ancient-set discriminators.
- [x] Corrected ancient/set bonus activation, including Rage Fighter sets.
- [x] Added configuration-update paths for the corrected persisted data.
- [x] Added or extended initialization tests for fresh and existing databases.

Relevant areas:

- `src/GameLogic/Attributes`
- `src/Persistence/Initialization/VersionSeasonSix/Items`
- `src/Persistence/Initialization/Updates`

### Jewels, item options, and Chaos Machine

- [x] Jewel of Life supports the Season 6 option ranges and failure reset rules.
- [x] Jewel of Harmony application, weighted option selection, restoration, and
  refining-stone removal paths were corrected.
- [x] The level-380 option mix requires a real target item and validates its
  eligibility.
- [x] Talisman of Luck is available to +10 through +15 combinations and adds the
  configured success percentage.
- [x] Talisman of Chaos Assembly (`group 14`, `number 96`) protects the target on
  failure and resets it to +0.
- [x] Talisman of Luck and Talisman of Chaos Assembly cannot be used together.
- [x] Seed Sphere mounting rejects a non-existing or occupied socket.
- [x] Seed Sphere removal rejects a non-existing or empty socket.
- [x] Existing databases are repaired by version 2 of the item-upgrade talisman
  configuration update.

Relevant areas:

- `src/GameLogic/PlayerActions/Craftings`
- `src/GameLogic/PlayerActions/Items/BaseItemCraftingHandler.cs`
- `src/Persistence/Initialization/VersionSeasonSix/ChaosMixes.cs`
- `src/Persistence/Initialization/Updates/AddTalismanOfLuckUpdatePlugIn.cs`

### Events and runtime safety

- [x] Golden Invasion timetable tests match the hourly configuration.
- [x] Event timetable collision checks distinguish entrance events from
  invasions which do not open an entrance.
- [x] Unsupported or corrupted mini-game reward enum values are logged and
  skipped instead of terminating the whole reward process.
- [x] Remote-view test fixtures initialize the required feature-plugin container.

### Character classes and master skills

- [x] Missing Season 6 master-tree data for Dark Knight, Dark Wizard, Elf,
  Dark Lord, Summoner, and Rage Fighter was added or repaired through
  initialization updates.
- [x] Rage Fighter multiple-hit skill behavior and ancient sets have targeted
  fixes and tests.

## Remaining work

### P0 - required before a production-ready claim

These are release-validation tasks. Passing unit tests alone cannot close them.

1. **Original-client compatibility test**

   Run the ENG 1.04d client through login, character selection, map changes,
   inventory movement, shop, trade, guild, duel, Gens, Mu Helper, Chaos Machine,
   and every supported event. Capture packet traces and verify that no extended
   packet is sent to the original client.

2. **Persistent database upgrade rehearsal**

   Restore a copy of a real pre-update database, run every mandatory
   configuration update, restart all services, and verify that a second update
   run is idempotent. Test rollback from a database backup as well.

3. **Concurrency and soak test**

   Run multiple game-server instances for at least 24 hours with concurrent
   login, reconnect, party, guild, trade, item crafting, event entry, death, and
   map-change traffic. Monitor deadlocks, unobserved task exceptions, memory
   growth, persistence conflicts, and duplicated rewards/items.

4. **Exploit and economy test**

   Attempt duplicate item moves, disconnect during crafting, repeated reward
   claims, invalid socket indexes, malformed helper settings, excessive packet
   rates, negative/overflow money operations, and Castle Siege tax races.

5. **Operations readiness**

   Document backups, restore drills, log retention, metrics, alerting, secret
   storage, process supervision, and a tested deployment/rollback procedure.

### P1 - gameplay implementation and parity gaps

1. **Illusion Temple runtime**

   The maps, ticket crafting, and cash-shop data exist, but there is no
   `IllusionTempleContext` or corresponding state-machine implementation under
   `src/GameLogic/MiniGames`. Implement team assignment, relic transport,
   skills, scoring, rewards, disconnect/rejoin behavior, and the complete reset
   loop before advertising this event as supported.

2. **Event state-machine integration tests**

   Blood Castle, Devil Square, Chaos Castle, Doppelganger, Imperial Guardian,
   Kanturu, and Raklion have runtime implementations, but each still needs a
   deterministic full-cycle test covering:

   - scheduled start and entrance closing;
   - all spawn/state transitions;
   - player death, disconnect, and rejoin;
   - success and timeout paths;
   - reward delivery exactly once;
   - map disposal and the next scheduled start.

3. **Boss and invasion parity tests**

   Add end-to-end tests for Selupan, Maya/Nightmare, Medusa, and every Golden
   Invasion tier. Validate spawn coordinates, skills, announcements, ownership,
   drops, cleanup, and restart recovery against observed 1.04d behavior.

4. **Chaos Castle drop configuration**

   `src/GameLogic/MiniGames/ChaosCastleDropGenerator.cs` still contains a note
   that part of its behavior should be configuration-driven. Move the remaining
   fixed selection rules into Season 6 configuration and add an update plugin.

5. **Socket bonus combinations**

   Mount/remove validation is covered, but the complete Season 6 elemental
   sequence matrix and bonus socket activation still require a data audit and
   exhaustive tests for Seed Sphere levels 1 through 5.

6. **Item database parity**

   Perform a machine-readable comparison of every Season 6 item definition:
   group/number, dimensions, slot, skill, durability, requirements, base stats,
   class permissions, drop level, excellent eligibility, ancient membership,
   harmony eligibility, 380 eligibility, and socket count. Manual spot checks do
   not prove the full catalog.

7. **Excellent-option probability distribution**

   Add deterministic/statistical tests for configured 1-6-option generation,
   uniqueness, maximum option count, and drop-group probabilities. Verify Life
   and Mana recovery/steal effects during real attacks, including capped values
   and multi-hit skills.

8. **Mu Helper protocol and economy integration**

   Packet handlers and server-side helper logic exist. Add an original-client
   trace test for settings round-trip, Zen charging intervals, insufficient-Zen
   shutdown, reconnect restoration, auto-potion, skill selection, repair, and
   pickup filters.

9. **Guild, Gens, duel, and Castle Siege full-cycle validation**

   These systems have substantial implementations, but require multi-player and
   multi-server integration tests. Castle Siege especially needs a complete
   registration-to-ownership cycle, crown/switch race tests, NPC persistence,
   tax concurrency, reward recovery, and restart during every schedule state.

### P2 - hardening and maintainability

1. Review the remaining gameplay TODOs, including the missing Archer and
   Spearman merchant entries in
   `src/Persistence/Initialization/VersionSeasonSix/MerchantStores.cs` and the
   per-level wing requirement note in
   `src/Persistence/Initialization/VersionSeasonSix/Items/Wings.cs`.
2. Replace the remaining non-configurable Chaos Castle selection values.
3. Add fuzz tests for client packet boundaries and invalid persisted enum values.
4. Add performance budgets for combat calculations, item generation, event
   waves, navigation, and persistence saves.
5. Remove or document intentionally skipped initialization tests before release.

## Recommended execution order

1. Implement Illusion Temple or explicitly disable its entry/ticket exposure.
2. Add deterministic full-loop tests for Kanturu, Doppelganger, Imperial
   Guardian, and Raklion.
3. Complete socket bonus sequence and excellent-option distribution audits.
4. Run the full item catalog comparison and repair data through initialization
   plus configuration-update plugins.
5. Validate Mu Helper with original-client packet captures.
6. Run Guild/Gens/Duel/Castle Siege multi-server scenarios.
7. Complete the P0 database, soak, exploit, and operations rehearsals.

## Database initialization and upgrades

- A fresh database receives the current values through Season 6 initialization.
- An existing database must run all pending mandatory configuration updates from
  the admin panel or the configured update workflow.
- Do not recreate a production database merely to obtain corrected initialization
  data.
- Back up the database before applying updates, and verify a second update scan
  reports no pending version of an already installed update.

## Definition of done

The Season 6 server can be called production-ready only when all P0 items are
completed, no enabled gameplay feature has an unimplemented runtime path, the
P1 event cycles pass repeatable integration tests, and a release candidate
survives the soak and upgrade rehearsal without item duplication, reward
duplication, deadlock, unbounded memory growth, or protocol incompatibility.
