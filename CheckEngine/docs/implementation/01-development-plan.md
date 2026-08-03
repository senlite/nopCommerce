# Check Engine Development Plan

## Objective
Start implementation with the first Horizon 1 slice defined by the product backlog: `EP-02 Scaffolding`.

## Preconditions
- Host upgraded to nopCommerce `4.90.6`
- Runtime on `.NET 9`
- Build and regression gates green after Horizon 0

## Phase 1 - Scaffolding (`EP-02`)
1. Create the four-project skeleton:
   - `src/Plugins/TwinParticles.CheckEngine/`
   - `src/Plugins/TwinParticles.CheckEngine.Domain/`
   - `src/Plugins/TwinParticles.CheckEngine.Application/`
   - `src/Plugins/TwinParticles.CheckEngine.Infrastructure/`
2. Add `plugin.json`
3. Add `CheckEngineStartup : INopStartup`
4. Add root settings with nested options
5. Add minimal plugin install/uninstall lifecycle
6. Add empty migration baseline
7. Add unit and architecture test projects
8. Add CI build/test gates

## Phase 2 - Vehicle foundation (`EP-03`) ? Completed
1. Vehicle hierarchy schema ?
2. Vehicle aggregates and invariants ? (baseline modeled through domain entities and service contracts)
3. Admin CRUD ? (full CRUD scaffold for Make/Model/Generation/Body/Engine/Market/Configuration/Alias)
4. Brand-agnostic seed loader ? (basic seed loader scaffold)

## Phase 3 - Parallel foundations
- VIN (`EP-04`) ? Completed (items 16-20)
- OEM (`EP-05`) ? Completed (items 21-25)

## Phase 4 - Core differentiator
- Fitment (`EP-06`) Completed scaffold (items 30-35)

## Phase 5 - Critical path
- Import pipeline (`EP-09`) Completed scaffold (items 26-29, 36-43)

## Current implementation snapshot
- See `CheckEngine/docs/implementation/05-current-status.md` for completed vs pending backlog slices.
