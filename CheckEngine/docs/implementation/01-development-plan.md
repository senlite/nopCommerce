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

## Phase 2 - Vehicle foundation (`EP-03`)
1. Vehicle hierarchy schema
2. Vehicle aggregates and invariants
3. Admin CRUD
4. Brand-agnostic seed loader

## Phase 3 - Parallel foundations
- VIN (`EP-04`)
- OEM (`EP-05`)

## Phase 4 - Core differentiator
- Fitment (`EP-06`)

## Phase 5 - Critical path
- Import pipeline (`EP-09`) starts as soon as vehicle and OEM foundations exist
