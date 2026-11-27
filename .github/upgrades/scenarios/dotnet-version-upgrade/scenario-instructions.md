## Scenario
- **Target Framework**: `net10.0`
- **Solution**: `x8086NetEmu.sln`

## Strategy
**Selected**: Hybrid
**Rationale**: The retained solution is heterogeneous: the `Eto.Forms` projects already target modern .NET, while the remaining VB projects still target `.NET Framework 4.8.1`, require SDK-style conversion, and sit on different points of the dependency chain. Excluding `x8086NetEmuWinForms` removes the heaviest UI migration hotspot, but the remaining work still benefits from dependency-ordered groups rather than a single bulk retarget.

### Execution Constraints
- Upgrade dependency roots before dependent projects, starting with `x8086NetEmu.vbproj`.
- Keep `x8086NetEmuWinForms` out of the modernization path and avoid spending migration effort on it.
- Treat the already-modern `Eto.Forms` projects as an alignment group that follows the shared library upgrades.
- Validate each group before moving to the next, then run full retained-solution build and test validation.
- Commit after each completed phase so each validated group can be reviewed independently.

## Preferences
### Flow Mode
- **Mode**: Automatic

### Commit Strategy
- **Mode**: After Each Phase

### Source Control
- **Source Branch**: `Eto.Forms`
- **Working Branch**: `upgrade-to-NET10`
- **Pending Changes Handling**: Committed before starting scenario
- **Branch Sync**: Disabled for now; user will test first and handle the eventual PR/merge manually

## User Preferences
### Technical Preferences
- **Upgrade Target**: Assess feasibility for `.NET 10`
- **UI Strategy**: Discard `x8086NetEmuWinForms` and use the `Eto.Forms` variants going forward
- **ROM Validation Path**: Use `Release\roms\` for emulator ROM-dependent runtime validation

### Execution Style
- **Flow**: Automatic
- **Branch Integration**: Do not merge/rebase into the source branch yet; leave final PR and merge to the user after their testing

## Key Decisions Log
- Initialized `.NET version upgrade` scenario targeting `net10.0` from branch `Eto.Forms` onto `upgrade-to-NET10` after saving pending changes.
- User chose to drop the WinForms version and treat the `Eto.Forms` projects as the forward path for modernization.
- Selected the `Hybrid` upgrade strategy so the legacy VB projects can be modernized in dependency order while the already-modern `Eto.Forms` projects are aligned afterward.
- User clarified that required emulator ROMs are expected under `Release\roms\` and runtime validation should use that location.
- User asked to avoid syncing the working branch with the source branch for now so they can test first and handle the eventual PR/merge themselves.
