# Changelog

All notable changes to Check Engine™ — both the product and this documentation baseline — are recorded
in this file.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). Version numbers
follow [Semantic Versioning 2.0.0](https://semver.org/), with the platform-compatibility discipline
described in [README.md](README.md#versioning-and-support).

---

## How to read this file

Check Engine has two version tracks, deliberately kept separate so that a documentation revision never
implies a software release.

| Track | Format | Meaning |
|---|---|---|
| **Documentation** | `0.x.y` | The specification baseline. Pre-1.0 until the full document set is complete and signed off. |
| **Software** | `1.x.y` and later | Released plugin builds. Begins at 1.0.0 on general availability. |

Change categories used below:

| Category | Applies to |
|---|---|
| `Added` | New documents, features, endpoints, or requirements |
| `Changed` | Modified behaviour, revised specifications, altered defaults |
| `Deprecated` | Still present, scheduled for removal, with a migration path |
| `Removed` | Deleted features or withdrawn requirements |
| `Fixed` | Corrected defects or specification errors |
| `Security` | Vulnerability remediation and hardening |
| `Decisions` | Architecture and product decisions recorded as ADRs |

Every entry that affects a requirement cites its identifier, so the changelog participates in the
traceability chain described in [README.md](README.md#traceability).

---

## [Unreleased]

Documentation work in progress. Phases are defined in [ROADMAP.md](ROADMAP.md#documentation-phases).

### Added

- **Software — plugin `0.95.0` self-hosted fonts (UX.9):** Outfit and IBM Plex
  Sans / Sans Arabic / Mono ship as woff2 under `Content/fonts`. Storefront
  chrome, theme head, portals, and marketplace no longer call
  `fonts.googleapis.com`. Lighthouse CWV evidence still open.
- **Software — plugin `0.94.0` / theme templates (UX.9):** Check Engine theme
  overrides category, catalog search, simple and grouped PDP, and cart. Fitment
  band renders inside the buy box; aftermarket label on PDP (`FR-904`). Storefront
  chrome no longer loads Google Fonts CDN (LCP). CWV evidence still open.
- **Software — plugin `0.93.0` / theme `CheckEngine` (UX.9 partial):** nopCommerce
  theme package at `src/Presentation/Nop.Web/Themes/CheckEngine`. Dark graphite
  shell over DefaultClean structure, Check Engine wordmark, homepage drops
  Welcome-to-our-store / news / polls, plugin activates the theme when the store
  is still on DefaultClean, footer affiliation disclaimer (`FR-901`). Category,
  PDP buy-box, and cart still use DefaultClean layout under the overlay.
- **Software — plugin `0.92.0` Figma UX.1–UX.8:** garage sheet replaces
  `window.prompt` VIN add; chip opens the sheet with real `aria-controls` /
  `aria-expanded`; rail masks VIN to last 4 (`FR-213`); in-sheet remove confirm
  (`AC-23.3` / `FR-714`); plugin CSS hides host DefaultClean search when
  `.ce-rail` is present; “Include unverified fit” moves under More filters;
  admin Order Inspector deep-links to `#ce-order-inspector`; uninstall card
  sits below portals; `--ce-radius-pill` token and mega `--ce-shadow-raised`.
  Full theme package (UX.9) and host Arabic keys (UX.10) are not in this slice.
- **Documentation — Figma-style UI/UX review:**
  [08 Figma-style UI/UX review](docs/implementation/08-figma-ui-ux-review.md)
  scores the in-tree plugin chrome and DefaultClean host against docs 21–23.
  Top gaps at review time: no Check Engine theme package, dual search,
  `window.prompt` VIN add, false `aria-haspopup="dialog"`, full VIN in the
  selector, Arabic host raw keys. Figma MCP was unauthenticated in the review
  environment. Review was of plugin `0.91.0`; UX.1–UX.8 ship in `0.92.0`.
- **Software — EXECUTION-PLAN Tracks 0–7 complete (engineering):**
  - Deterministic build scripts (`CheckEngine/scripts/build-checkengine.sh|.ps1`) and CI wiring
  - SQL product search, storefront widgets (search/fitment/garage), admin dashboard
  - Import SQL persistence (`CorrelationId`), Excel/PDF extractors, `NopImportProductPublisher`
  - AI completion ports (Null + OpenAI-compatible), unpublished proposal workflow
  - ERPNext HTTP adapter with stub fallback; fitment-constrained recommendations
  - Audit events, health/diagnostics endpoints, perf/resilience/traceability gates
  - Operator runbook + acceptance go/no-go (`docs/implementation/06`, `07`)
- **Phase 2 — Architecture and domain model** (documentation baseline continuation):
  - [08 System Architecture](docs/08-system-architecture.md) — Clean Architecture layers, module map,
    cross-cutting concerns, deployment topologies, host-internal vs Horizon 5 public API, and ADRs
    `ADR-011`–`ADR-014`
  - [09 Plugin Architecture](docs/09-plugin-architecture.md) — four-project plugin shape, DI composition
    root, FluentMigrator, routes, widgets, consumers, schedule tasks, permissions, install/update/uninstall
  - [10 Database Design](docs/10-database-design.md) — `Ce*` schema, ER model, indexes, host FK policy,
    migration and uninstall drop strategy
  - [11 Domain Model](docs/11-domain-model.md) — aggregates, value objects, domain services, invariants
    `INV-001`–`INV-015`, domain events
  - [34 Coding Standards](docs/34-coding-standards.md) — nullable, async, Options, CQRS boundaries,
    error handling, analyser policy, fitment fail-closed review checklist
- **Phase 3 — Automotive core engines:**
  - [12 Vehicle Database](docs/12-vehicle-database.md) — brand-agnostic hierarchy, configuration
    fingerprint, aliases, curation, selector rules (`FR-101`–`FR-130`)
  - [13 VIN Engine](docs/13-vin-engine.md) — ISO check digit, pluggable decoders, confidence,
    privacy logging (`FR-201`–`FR-215`)
  - [14 OEM Engine](docs/14-oem-engine.md) — normalisation, supersession, aftermarket equivalence,
    product maps (`FR-220`–`FR-240`)
  - [15 Fitment Engine](docs/15-fitment-engine.md) — evaluation outcomes, qualifiers, publication
    policy, safety hard stop (`FR-301`–`FR-330`)
  - [16 Search Engine](docs/16-search-engine.md) — unified six-mode contract, bilingual index,
    fitment filter, fallback (`FR-401`–`FR-450`)
- [10 Database Design](docs/10-database-design.md) — `CeVehicleAlias` table for `FR-107`
- **Phase 4 — Data acquisition and AI:**
  - [17 AI Architecture](docs/17-ai-architecture.md) — provider port, disclosure, prompts, ceilings,
    degradation, assistant/recommendation safety (`FR-501`–`FR-590`)
  - [24 Product Import Pipeline](docs/24-product-import-pipeline.md) — twelve-stage PDF/Excel/CSV
    ingest through review and publish (`FR-601`–`FR-650`)
  - [25 AI Content Pipeline](docs/25-ai-content-pipeline.md) — description, specs, glossary
    translation, SEO candidates and review (`FR-520`–`FR-523`, `FR-620`–`FR-622`)
  - [26 Image Management](docs/26-image-management.md) — media pipeline, derivatives, placeholders,
    replacement, quarantine (`FR-630`–`FR-631`, `FR-660`–`FR-670`)
- [10 Database Design](docs/10-database-design.md) — `CeAiUsageDaily`, `CeSupplierProfile`
- **Phase 5 — Integration and multi-party operation:**
  - [18 ERPNext Integration](docs/18-erpnext-integration.md) — entity sync matrix, stock SoR,
    outbox/idempotency, conflicts, reconciliation, downtime-safe checkout (`FR-801`–`FR-830`)
  - [19 Marketplace Module](docs/19-marketplace-module.md) — vendor isolation, commissions, split
    carts, vendor fitment, licence gating (`FR-850`–`FR-890`, Horizon 3)
  - [20 Customer Garage](docs/20-customer-garage.md) — active vehicle, guest migrate, privacy,
    widget/API contracts (`FR-701`–`FR-720`)
- [10 Database Design](docs/10-database-design.md) — `CeErpEntityMap`, `CeErpSyncOutbox`, `CeErpConflict`
- **Phase 6 — Experience and discoverability:**
  - [21 Theme Design](docs/21-theme-design.md) — dark automotive theme, sticky search, garage chrome,
    PDP fitment band, landing templates, CWV-oriented composition
  - [22 UI Design System](docs/22-ui-design-system.md) — tokens, Outfit/IBM Plex (+ Arabic), fitment
    semantics, component catalogue
  - [23 UX Guidelines](docs/23-ux-guidelines.md) — WCAG 2.2 AA, RTL, honesty rules, zero-result recovery
  - [27 SEO Strategy](docs/27-seo-strategy.md) — vehicle/part landings, hreflang, structured data,
    sitemaps, noindex thin pages (`FR-430`–`FR-445`)
- **Phase 7 — Production operation:**
  - [28 Security](docs/28-security.md) — authz, OWASP mapping, rate limits, secrets, VIN redaction,
    disclosure (`NFR-033`–`NFR-045`)
  - [29 Performance](docs/29-performance.md) — budgets, caching, N+1 gates, load/CWV methodology
  - [30 Analytics](docs/30-analytics.md) — event taxonomy, funnels, fitment accuracy, privacy-safe VIN
  - [31 Logging](docs/31-logging.md) — structured logs, correlation, redaction, diagnostics packages
  - [32 Deployment](docs/32-deployment.md) — 4.60→4.90.6 track, install/upgrade/rollback, web farm
  - [33 CI-CD](docs/33-ci-cd.md) — GitHub Actions gates, Mermaid lint, packaging, SemVer alignment
  - [35 Testing Strategy](docs/35-testing-strategy.md) — TDD, coverage floors, fitment corpus, a11y/perf
- **Phase 8 — Delivery and product evolution:**
  - [36 Sprint Planning](docs/36-sprint-planning.md) — capacity model, Horizon 0/1 sprint maps, DoR/DoD, corpus-gated freeze
  - [37 Product Backlog](docs/37-product-backlog.md) — ranked H0/H1 backlog, WSJF-lite ordering, H2–H5 icebox
  - [38 Epics](docs/38-epics.md) — `EP-01`–`EP-28` mapped to horizons, BRs, and exit criteria
  - [39 User Stories](docs/39-user-stories.md) — `US-001`–`US-120` delivery inventory (module-local US tables remain illustrative)
  - [40 Acceptance Criteria](docs/40-acceptance-criteria.md) — Given/When/Then for Must H0/H1, critical safety ACs, H2–H5 skeletons
  - [41 Release Plan](docs/41-release-plan.md) — v1.0–v2.0 gates, beta programme, .NET 10 milestone (`ADR-002`)
  - [42 Marketplace Publishing](docs/42-marketplace-publishing.md) — nopCommerce Marketplace listing and update process
  - [43 Licensing](docs/43-licensing.md) — tiers, activation, entitlement degrade (`ADR-009`), support/EOL
  - [44 Commercial Strategy](docs/44-commercial-strategy.md) — packaging, indicative pricing, partners, unit economics
  - [45 Future Roadmap](docs/45-future-roadmap.md) — committed / evaluated / rejected beyond Horizon 5
  - [46 Workshop Portal](docs/46-workshop-portal.md) — jobs, trade pricing, fitment via core only (`FR-1000s`)
  - [47 Fleet Portal](docs/47-fleet-portal.md) — registers, forecasting, approval workflows (`FR-1100s`)
  - [48 Dealer Portal](docs/48-dealer-portal.md) — franchise catalog, quota, warranty (`FR-1200s`)
  - [49 SaaS Roadmap](docs/49-saas-roadmap.md) — multi-tenant options, metering, data-as-a-service (`FR-1300s`)
  - [Appendix](docs/appendix.md) — glossary, vocabulary governance, ADR-001–015 index, bibliography

### Fixed

- Corrected Mermaid syntax errors across the Phase 1 document set. All 34 diagrams now parse under the
  enforced allowlist (`flowchart`, `graph`, `sequenceDiagram`, `erDiagram`, `stateDiagram-v2`,
  `classDiagram`, `gitGraph`, `pie`):
  - `06-personas.md` — `subgraph TB` used the reserved direction keyword `TB` as an identifier; renamed
    to `TRD`. This was a hard parse failure.
  - `07-user-journey.md` — a raw `<` in the node label `["Decode < 40 ms local"]` was parsed as an HTML
    tag; reworded to `["Decode under 40 ms local"]`. Separately, the J1 `journey` diagram failed to
    render in the reader's toolchain; replaced with a staged `flowchart` plus a satisfaction score
    table that preserves the original 1–5 scores.
  - `ROADMAP.md` — replaced the `timeline` diagram, which relied on continuation-line `:` syntax and
    labels beginning with a period, with an equivalent `flowchart`.
  - `00-vision.md` — replaced the `mindmap` product-pillar diagram with an equivalent `flowchart`, as
    `mindmap` is indentation-sensitive and unreliable on GitHub.
  - `01-business-requirements.md` — replaced the residual-risk `quadrantChart`, whose point labels
    contained hyphens (`RISK-01`) that conflict with the axis `-->` token, with a `flowchart` grouping
    risks into residual bands. The previous chart also stacked most points on identical coordinates.
  - `04-competitive-analysis.md` — the remaining `quadrantChart` still failed to render; replaced with
    a banded `flowchart` of competitive positions and an adjacent scored-position table. Also renamed
    a node that would have collided with the reserved direction keyword `TD`.
  - `README.md` and `CONTRIBUTING.md` — removed slashes, dots, and colons from `gitGraph` branch names
    and commit ids, which the gitGraph lexer handles unreliably. The real branch naming convention
    remains documented in the adjacent tables.
  - `README.md` — simplified an edge label that combined `<br/>` with parentheses.
- `06-personas.md` — fixed a broken table-of-contents anchor caused by an en dash in a heading; the
  heading is now "Persona to requirement trace matrix".

### Changed

- [CONTRIBUTING.md](CONTRIBUTING.md#mermaid-diagram-standards) — tightened the Mermaid standards. The
  diagram-type allowlist is now enforced rather than advisory: `mindmap`, `timeline`, `journey`, and
  `quadrantChart` are explicitly banned. Satisfaction scores and competitive positions move into
  adjacent tables. Reserved-identifier, angle-bracket, and `gitGraph` naming rules remain.
- [docs/README.md](docs/README.md) — Tracks updated through Phase 8 documents marked Review.
- [ROADMAP.md](ROADMAP.md#documentation-phases) — Phases 1–8 Complete; documentation baseline pending sign-off.
- [34 Coding Standards](docs/34-coding-standards.md) — adopted **Test-Driven Development** as mandatory
  methodology (`ADR-015`): red→green→refactor, scope table, anti-patterns, review checklist, and
  acceptance criteria `AC-34.6` / `AC-34.7`.
- [CONTRIBUTING.md](CONTRIBUTING.md#test-driven-development) — TDD section, PR verification evidence,
  definition-of-done items, and PR template checklist updates.

### Decisions

| ID | Decision | Rationale |
|---|---|---|
| `ADR-011` | Use nopCommerce `IRepository<T>` / LinqToDB for Check Engine tables | Avoids a second ORM beside the host |
| `ADR-012` | Composition root in Host may reference Infrastructure; other Host code talks to Application only | Preserves testability without paralysing DI |
| `ADR-013` | Fitment evaluation synchronous on request path; batch re-eval async | Meets badge and search latency NFRs |
| `ADR-014` | Search index is a projection; fitment claims remain system of record | Prevents index drift from becoming false Fits |
| `ADR-015` | Adopt Test-Driven Development (red→green→refactor) for production behaviour | Executable specification before implementation; required for Domain/Application, bug fixes, and contract changes. Coverage floors alone are insufficient |

### Planned

- Product-owner sign-off of documents marked Review, promoting the documentation baseline to 1.0.0
  when Approved

---

## [0.1.0] — 2026-07-28

**Documentation baseline, Phase 1 — Product foundation and strategy.**

The first published specification baseline. Establishes the product definition, the commercial and
legal framing, the complete requirement set, and the user model. This is the layer every subsequent
document depends on: identifiers assigned here are referenced by all later phases and are stable.

### Added

**Repository foundation**

- `README.md` — product overview, the fitment problem statement, module inventory, platform
  requirements, architecture summary, packaging model, documentation map with role-based reading
  paths, identifier scheme, traceability model, glossary, and the versioning and support matrix
- `LICENSE.md` — the commercial end-user licence agreement, comprising twenty operative sections plus
  a documentation licence and third-party notices appendix. Covers the tier model, restrictions,
  source-code provisions, activation and offline operation, the fitment accuracy limitation, the
  trademark nominative-use allocation, AI feature terms, data protection roles, warranty, liability
  caps, indemnities, and audit rights
- `CHANGELOG.md` — this file
- `ROADMAP.md` — the delivery roadmap across five product horizons and the eight documentation phases
- `CONTRIBUTING.md` — branching model, commit conventions, the mandatory document section template,
  review gates, and the definition of done for both code and documentation
- `docs/README.md` — the annotated document index

**Specification documents**

| Document | Establishes |
|---|---|
| [00 Vision](docs/00-vision.md) | Product vision, the problem thesis, positioning, the brand-agnostic principle, and success measures |
| [01 Business Requirements](docs/01-business-requirements.md) | 42 business requirements `BR-001`–`BR-042`, the market and revenue model, stakeholder map, constraints, assumptions, and 14 tracked risks `RISK-01`–`RISK-14` |
| [02 Functional Requirements](docs/02-functional-requirements.md) | 214 functional requirements across nine module blocks, `FR-101`–`FR-914`, each traced to a business requirement |
| [03 Non-Functional Requirements](docs/03-non-functional-requirements.md) | 68 non-functional requirements `NFR-001`–`NFR-068` covering performance budgets, scalability, availability, security, accessibility, localisation, and maintainability, each with a measurement method |
| [04 Competitive Analysis](docs/04-competitive-analysis.md) | Assessment of eleven competing approaches across four categories, feature matrices, pricing comparison, and the defensibility analysis |
| [05 Product Strategy](docs/05-product-strategy.md) | Positioning statement, the market-agnostic regional provider strategy, segment prioritisation, moat construction, and the build-versus-license decision record |
| [06 Personas](docs/06-personas.md) | Eleven personas across four classes — end customer, trade buyer, operator, and administrator — with goals, frustrations, technical context, and the requirements each drives |
| [07 User Journey](docs/07-user-journey.md) | Fourteen end-to-end journeys with stage-by-stage maps, emotional arcs, failure modes, instrumentation points, and success criteria |

### Decisions

Architecture and product decisions recorded in this phase. Full records are in the
[Appendix](docs/appendix.md).

| ID | Decision | Rationale |
|---|---|---|
| `ADR-001` | Target nopCommerce 4.90.6 on .NET 9 | Current supported platform line. Supersedes the initial 4.30 assumption, which targets .NET Core 3.1, end-of-life since December 2022 |
| `ADR-002` | Schedule a .NET 10 retargeting milestone for the first year | .NET 9 is a Standard Term Support release ending 10 November 2026. nopCommerce's next major version is expected on .NET 10 LTS. Treated as budgeted work, not an emergency |
| `ADR-003` | Curate vehicle, OEM, and fitment data in-house | Removes per-seat data licensing cost and redistribution restriction, making the SaaS horizon viable and the catalog an owned asset. Accepts data quality as the principal product risk, mitigated by confidence scoring, provenance, and human review |
| `ADR-004` | Market-agnostic core with pluggable regional providers | Paymob and Bosta are reference implementations of the payment and shipping provider patterns, not required dependencies. Keeps the addressable market global |
| `ADR-005` | One plugin for the platform, separate plugins for payment and shipping | Matches nopCommerce's provider group model, allows independent versioning of regional integrations, and keeps the core installable without regional dependencies |
| `ADR-006` | Fitment as a first-class relation, not an attribute or a category | The combinatorial explosion of variant SKUs and the ambiguity of free-text fitment are the two failure modes the product exists to eliminate |
| `ADR-007` | Domain layer takes no dependency on nopCommerce assemblies | Makes vehicle, VIN, OEM, and fitment logic unit-testable without a host and portable across platform major versions |
| `ADR-008` | Every AI feature disabled by default, individually toggleable | AI features transmit data to a third-party provider and incur per-call cost. Explicit opt-in with per-feature data disclosure is the only defensible default |
| `ADR-009` | Licence expiry never interrupts commercial operation | A lapsed licence degrades administration and background services to read-only but never takes a storefront offline. Removes the largest objection to entitlement enforcement |
| `ADR-010` | Documentation filenames use lowercase kebab-case | Filenames containing spaces require percent-encoding in Markdown links, break shell tooling without quoting, and are mishandled by several static site generators. Numeric prefixes are retained for ordering |

### Notes

- **Filename convention.** The document set was specified with space-separated filenames such as
  `00 Vision.md`. Files are created as `00-vision.md` for link and tooling safety, per `ADR-010`. The
  numbering and titles are otherwise unchanged, and [docs/README.md](docs/README.md) carries the
  mapping.
- **Platform version.** The specification originally named nopCommerce 4.30. Documents target 4.90.6
  per `ADR-001`. This is a material change: 4.30 runs on .NET Core 3.1, which has been out of support
  since 13 December 2022, and shipping a new commercial product against an unpatched runtime was not
  defensible.
- **Host tree.** This repository resides in a nopCommerce 4.60.4 source tree. Reaching 4.90 spans
  three major versions with plugin-facing breaking changes at each step. The upgrade is a prerequisite
  track, not parallel work, and is sequenced in
  [36 Sprint Planning](docs/36-sprint-planning.md).

---

## Release entry template

Future software releases use the following structure. Retained here so that entries stay consistent.

```markdown
## [1.0.0] — YYYY-MM-DD

**General availability.** Requires nopCommerce 4.90.0–4.90.6 on .NET 9.

### Added
- Feature description (`FR-nnn`, `US-nnn`)

### Changed
- Behaviour change, with migration guidance (`FR-nnn`)

### Deprecated
- Feature scheduled for removal in x.y.z, with replacement (`FR-nnn`)

### Removed
- Removed feature and the release that deprecated it (`FR-nnn`)

### Fixed
- Defect corrected, with the affected versions (`ISSUE-nnn`)

### Security
- Hardening or remediation, with severity and CVE where applicable (`NFR-nnn`)

### Upgrade notes
- Schema migrations applied and their expected duration on a reference dataset
- Configuration keys added, renamed, or removed
- Manual steps required before or after upgrade
- Index rebuild requirements
```

### Entry rules

1. **Every entry cites an identifier.** A change with no traceable requirement, story, or issue is a
   change nobody asked for.
2. **Write for the operator, not the author.** State the effect on a running store, not the internal
   implementation.
3. **Breaking changes are called out explicitly**, with the migration path and an estimate of the
   effort required.
4. **Schema migrations are always listed in upgrade notes**, with measured duration against the
   reference dataset defined in [29 Performance](docs/29-performance.md).
5. **Security entries never disclose exploitable detail** before the coordinated disclosure window
   closes, per the policy in [28 Security](docs/28-security.md).
6. **Yanked releases** are marked `[YANKED]` with the reason and the superseding version.

---

## Version history index

| Version | Date | Type | Summary |
|---|---|---|---|
| [0.1.0](#010--2026-07-28) | 2026-07-28 | Documentation | Phase 1 baseline — product foundation and strategy |

---

## References

- [README.md](README.md) — versioning policy and support matrix
- [ROADMAP.md](ROADMAP.md) — delivery horizons and documentation phases
- [CONTRIBUTING.md](CONTRIBUTING.md) — change submission process
- [33 CI-CD](docs/33-ci-cd.md) — release automation and changelog validation
- [41 Release Plan](docs/41-release-plan.md) — release cadence and gating criteria
- [43 Licensing](docs/43-licensing.md) — support lifecycle and end-of-life policy
- [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/)
- [Semantic Versioning 2.0.0](https://semver.org/)
