# 05 Current Implementation Status

Status date: 2026-08-03

## Completed backlog slices

- `EP-02` Scaffolding ✅
- `EP-03` Vehicle foundation ✅
- `EP-04` VIN foundation ✅
  - 16 VIN value object + check digit
  - 17 WMI/VDS/VIS parsing + confidence model
  - 18 Reference decoder plugin (BMW scaffold)
  - 19 VIN decode API + rate limiting scaffold
  - 20 VIN graceful failure/unknown path
- `EP-05` OEM foundation ✅
  - 21 OEM registry schema + normalization
  - 22 Cross-reference/equivalence relation foundations
  - 23 Supersession chain service (directed + transitive)
  - 24 OEM admin linking scaffold
  - 25 OEM search integration hook (`ResolveOem` scaffold)
- `EP-06` Fitment core ✅ Completed scaffold
  - 30 Fitment claim aggregate + qualifiers model scaffold
  - 31 Production-date window evaluation scaffold
  - 32 Confidence + provenance scaffold
  - 33 Fail-closed evaluation scaffold
  - 34 Human review queue scaffold
  - 35 Caching + publish invalidation scaffold
- `EP-07` Unified search ✅ Completed scaffold
  - 49 Unified query contract scaffold
  - 50 VIN mode integration scaffold
  - 51 OEM mode integration scaffold
  - 52 Vehicle-tree/category/keyword mode scaffold
  - 53 Arabic-English normalization scaffold
  - 54 Ranking/faceting scaffold
  - 55 Zero-result recovery scaffold
  - 56 Rebuild/degraded fallback scaffold
- `EP-08` Garage ✅ Completed scaffold
  - 57 Garage aggregate for vehicles/VINs/OEMs scaffold
  - 58 Guest-to-account migration scaffold
  - 59 Active-vehicle scoped browsing integration scaffold
  - 60 Cross-device persistence scaffold (in-memory repository contract)
  - 61 Admin support view with audit scaffold
- `EP-09` Import pipeline ✅ Completed scaffold
  - 26 Extraction stage scaffold (CSV/Excel/PDF parser contracts + CSV parser implementation)
  - 27 Normalization stage scaffold
  - 28 Duplicate-detection stage scaffold
  - 29 OEM-matching stage scaffold
  - 36 Vehicle-matching stage scaffold
  - 37 AI enrichment hook scaffold
  - 38 Translation hook scaffold
  - 39 SEO-generation hook scaffold
  - 40 Categorisation stage scaffold
  - 41 Image-assignment stage scaffold
  - 42 Human review admin workflow scaffold
  - 43 Publication stage with dry-run/partial-success scaffold
- `EP-10` Image management ✅ Completed scaffold
  - 44 Image sourcing/storage scaffold via nop picture service adapter
  - 45 Derivative delivery variants scaffold (listing/product/zoom)
  - 46 CDN delivery integration scaffold
  - 47 Placeholder fallback scaffold
  - 48 Professional replacement workflow scaffold
- `EP-11` Theme components ✅ Completed scaffold
  - 62 Theme layout/mega-menu widget-zone scaffold
  - 63 Theme garage widget scaffold
  - 64 Theme vehicle selector scaffold
  - 65 Theme sticky-search bar scaffold
  - 66 Theme product-page fitment band scaffold
  - 67 Theme landing template scaffold
- `EP-12` L10n/RTL foundations ✅ Completed scaffold
  - 68 Resource-key structure scaffold + localization preview keys
  - 69 RTL logical CSS properties scaffold
  - 70 Mirrored icon direction scaffold
  - 71 Arabic formatting/typography-friendly locale scaffold
  - 72 Number/date/unit locale formatting service scaffold

## In progress / not finished yet

### EP-09 Import pipeline
- No remaining EP-09 stage scaffolding items; future work is depth/production-hardening.

### Not started major epics
- Remaining later-horizon epics (`EP-13+`)

## Validation snapshot

- Latest CheckEngine architecture test suite run: **129 passed, 0 failed**.
- Validation has been run inside the CheckEngine test boundary repeatedly after each increment.

## Known open technical constraint

- Full host/plugin build is still blocked by unrelated framework ambiguity outside CheckEngine scope:
  - `Nop.Web.Framework` ambiguous `IPNetwork` type reference.
