# 08 Figma-style UI/UX review — plugin + theme

**Date:** 2026-09-02 · **Plugin at review:** `TwinParticles.CheckEngine` `0.91.0` · **Implementation:** `0.92.0`–`0.96.0` (UX.1–UX.9) · **Theme:** `CheckEngine` dark shell over DefaultClean structure

**Engineering status:** Design review plus the first implementation slice. Spec baselines are [21 Theme Design](../21-theme-design.md), [22 UI Design System](../22-ui-design-system.md), and [23 UX Guidelines](../23-ux-guidelines.md). Live progress is in [EXECUTION-PLAN.md](../../EXECUTION-PLAN.md). UX.9 (full theme) and UX.10 (host Arabic keys) remain pending / blocked.

**Figma MCP:** the Figma integration in this cloud session is `needsAuth`. This review was produced from live storefront/admin captures and the in-tree chrome, not from a Figma file. Authenticate Figma in Cursor Desktop and re-run if you want the same findings written onto FigJam / a file.

---

## Verdict

Check Engine’s **own chrome is a coherent dark automotive kit** (tokens, logical properties, 44 px targets, honest fitment copy, zero-result recovery). It is **not yet a theme**. Shoppers still see DefaultClean + the nopCommerce logo. Plugin `0.92.0` replaces `window.prompt` VIN add with a garage sheet, hides the host search when the CE rail is present, and deep-links Order Inspector. Admin remains a host AdminLTE link board, not an operator product. UX.9 (full theme) is still the remaining Horizon 1 visual gap.

**Ship-readiness for Horizon 1 storefront UX:** not gated. Fitment honesty and EN chrome are the strongest parts. The product still looks like a plugin dropped onto a demo store.

| Score (1–5) | Area |
|---|---|
| 4 | Fitment honesty and status language |
| 4 | Token discipline inside `.ce-root` |
| 3 | RTL of Check Engine chrome only |
| 2 | Theme / brand composition vs [21](../21-theme-design.md) |
| 2 | Garage / add-vehicle interaction vs [23](../23-ux-guidelines.md) |
| 2 | Admin information architecture |
| 1 | End-to-end bilingual store (host + catalog + chrome) |

---

## Method

Surfaces reviewed:

- Storefront chrome: `CheckEngineThemeChrome` (mega menu, rail, hero, fitment band, VIN modal markup)
- Tokens + theme CSS: `checkengine-tokens.css`, `checkengine-theme.css`
- Storefront JS: `checkengine-storefront.js`
- Admin: Dashboard, Configure, Vehicle admin, fitment/import/search boards
- Captures: G6 EN/AR desktop + 320 px, category, PDP, garage-after-VIN, search zero-state, admin dashboard/vehicle

Heuristic set: Nielsen, WCAG 2.2 AA (`NFR-046`), vehicle-context first object (`FR-705` / `FR-709`), fitment never invented (`FR-320`), RTL as first-class (`NFR-052`), design-system catalogue in [22](../22-ui-design-system.md).

---

## What is working

1. **Dark chrome is internally consistent.** Graphite surfaces, IBM Plex / Outfit, 4–64 px spacing, 4/8/12 radius, 44 px controls, visible `:focus-visible`.
2. **Fitment band is honest.** Empty state is “Select your vehicle,” not a speculative Fits. Unmatched VIN copy is “VIN not matched yet… fitment cannot be checked yet.” Colour is paired with icon + text (`AC-22.4`).
3. **Zero-result recovery exists.** “No matching parts found” plus two next steps (`AC-23.2` / `FR-412`).
4. **Logical CSS.** Rail, chip, search, and mega menu use logical properties; directional icons flip in RTL; OEM/VIN stay LTR isolate.
5. **Mobile rail is thought through.** Search takes the first row under 768 px; mega panel becomes a bottom sheet; control height drops to 40 px.
6. **Admin uninstall guard** is explicit (24-hour export). That is correct safety UX, even if it currently dominates the dashboard.

---

## Findings

Severity: **P0** blocks trust or a11y of a primary path · **P1** fails a documented Must / Horizon 1 template · **P2** quality / IA · **P3** polish.

### P0 — Add-vehicle is a browser `prompt`, not a garage sheet

**Surface:** Garage chip, vehicle `<select>`, hero “Add your vehicle,” mega footer, PDP fitment CTA.

**Evidence:** `promptForVin()` in `checkengine-storefront.js` calls `window.prompt`. The chip click only `focus()`es the select. The chip still advertises `aria-haspopup="dialog"`. A VIN disambiguation modal exists in markup, but first-run add never uses a designed field.

**Why it fails:** [22] catalogues “Modal / sheet — Selector, garage.” [23] requires keyboard VIN (`US-622`), no dark-pattern dead ends, and a visible labelled field. `window.prompt` has no check-digit feedback, no last-4 masking, no RTL layout, no cancel that stays in-page, and is blocked or ugly on many mobile browsers.

**Figma frame:** `Garage / Add vehicle` — one sheet: VIN field + helper + last-4 preview + “or pick make/model” + Cancel / Add. Do not use a native prompt.

---

### P1 — There is no Check Engine theme, only DefaultClean + widgets

**Surface:** Every public page.

**Evidence:** `src/Presentation/Nop.Web/Themes` contains only `DefaultClean`. Chrome comments say assets must not overlap the host header/logo. Live home still shows the nopCommerce wordmark, host “Search store,” DefaultClean type, then a dark CE island.

**Why it fails:** [21] specifies a **premium dark automotive theme** as the storefront, not a bar glued onto the host demo. Tokens never reach header, footer, category grid, PDP purchase block, or cart. Shoppers read two brands (nopCommerce + Check Engine) and two visual systems.

**Figma file:** `Theme / Shell` — dark header, wordmark, single search, footer, product tile, PDP buy box, all on `--ce-surface-*`. Widget-only overlays are a rejected alternative unless [21] is rewritten.

---

### P1 — Two search systems compete

**Surface:** Home, category, PDP, 320 px.

**Evidence:** Host “Search store” + CE “Search parts, OEM, or VIN” on every template. On 320 px they stack: host search, All parts, CE search, then garage.

**Why it fails:** [21] sticky search is **the** search (`FR-414`). Dual search splits intent (catalog vs VIN/OEM), doubles LCP candidates, and makes the host logo/search the LCP element (already why `NFR-002` is unmet).

**Figma frame:** One search in the rail. Host box hidden or restyled to the same control. Mobile: one field, then vehicle chip.

---

### P1 — Two vehicle controls, one job

**Surface:** Rail.

**Evidence:** Garage chip (“Select vehicle” / truncated VIN) sits next to a native select (“No vehicle selected” / full VIN). Chip does not open a dialog; it focuses the select.

**Why it fails:** [23] vehicle context is a **first-class object**. Two widgets with the same state is split attention. The select also leaks the **full VIN** after add (`FR-213` / `NFR-044`: last 4 or hash in UI).

**Figma frame:** One chip. Click opens the garage sheet. Active label is year/make/model or `VIN ···1234`, never the 17-character value.

---

### P1 — `aria-haspopup="dialog"` is false

**Surface:** `#ce-garage-chip`.

**Evidence:** Markup still has `aria-haspopup="dialog"` and `aria-expanded="false"`. Click focuses a `<select>`.

**Why it fails:** Screen-reader users are told a dialog will open. G6 already removed a similar garage-chip dialog claim from copy; the attribute remains.

**Fix:** Remove `aria-haspopup` until a real dialog exists; then wire `aria-expanded` / `aria-controls`.

---

### P1 — Marketplace “Order inspector” is the scoreboard

**Surface:** Admin dashboard.

**Evidence:** Both Scoreboard and Order Inspector `href`s are `/Admin/CheckEngine/VendorAdmin/Scoreboard`.

**Why it fails:** Broken IA. Operators cannot find the inspector. Duplicate links look like unfinished admin, not a product.

---

### P1 — Arabic storefront is not bilingual-complete

**Surface:** `/ar/` desktop and 320 px.

**Evidence:** CE rail, mega, and hero translate and flip. Host header on Arabic shows raw keys (`ACCOUNT.LOGIN`, `SEARCH.BUTTON`, `shoppingcart.headerquantity`). Home body stays “Welcome to our store.” Catalog titles mash Arabic + English on the EN locale.

**Why it fails:** [23] rejects “English-first, Arabic phase 2” (`NFR-051`). CE cannot claim an Arabic store if the shell is broken. Host keys are host-owned; the **experience** is still Check Engine’s storefront.

**Figma frames:** `AR / Header` and `EN / Catalog title` — one language per locale; bilingual only in a dedicated dual-script specimen, not production titles.

---

### P2 — Design tokens vs spec drift

| Spec [22] | In tree | Issue |
|---|---|---|
| `--ce-color-surface-raised` naming | `--ce-surface-raised` | Prefix scheme differs; document or alias |
| Component tokens (button, badge, input) | Only primitive + semantic | Layer 3 missing |
| No full pills on primary actions | `--ce-radius-pill` used on mega eyebrow | Token not defined in `checkengine-tokens.css` (theme.css line 146 has no fallback) |
| Fonts self-hosted / licensed in theme | Google Fonts CDN in chrome | LCP + spec assumption |
| Atmosphere: radial graphite → blue-black page | Hero island only; page is DefaultClean white | Theme gap |
| Contrast ≥ 4.5:1 primary button | `--ce-action-primary` `#0066B1` on `--ce-action-on-primary` `#E8EEF7` | Borderline; verify in Figma contrast checker |

---

### P2 — Admin is a link dump, not an operator product

**Surface:** Dashboard, Vehicle admin, Configure.

**Evidence:** Dashboard is grouped `<ul>` cards (better than a flat list) but still no status, counts, or primary task. Uninstall export is the first visual block. Vehicle admin is a level dropdown + empty table, not a tree. Configure is a long host form. Commission configure hard-codes `vendorId=1`.

**Why it fails:** [22] leaves admin skinning out of v1.0, but operators still need IA: “what is broken / what do I do today.” A yellow uninstall card as the hero is the wrong first impression.

**Figma frames:** `Admin / Home` — licence, health, pending reviews, last import. `Admin / Vehicle tree` — Make → Model → Generation, not eight table modes.

---

### P2 — Progressive disclosure is incomplete

**Surface:** Rail.

**Evidence:** “Include unverified fit” is always on the primary row. Search mode (NL / semantic) is correctly hidden unless enabled. Assistant is gated. Widen-fitment is expert language next to Search.

**Why it fails:** [23] progressive disclosure. Widen belongs in empty-state recovery or a “More filters” disclosure, not the first-run row.

---

### P2 — Hero is undercut by demo content

**Surface:** Home.

**Evidence:** Strong CE hero, then DefaultClean “Welcome to our store” and nopCommerce forum links. Category grid (dark cards, GMaster icons) is more “automotive” than home.

**Figma frame:** Home = hero + vehicle prompt + 3–6 category tiles. No host lorem.

---

### P2 — Mega menu is a single button, not navigation

**Surface:** `header_after`.

**Evidence:** “All parts” is a lone dark control. Host category menu is unused. Empty state is handled; populated state is four-column. On mobile the trigger is full-width and the panel is a bottom sheet (good).

**Why it fails:** [21] global chrome is header + mega + rail as one system. Isolated button reads as a widget, not a catalog.

---

### P3 — Visual polish

- Chip label + value stack makes “Garage / Select vehicle” look like two buttons.
- Select caret is a background SVG; RTL flips position (good) but native options still follow OS chrome.
- Mega panel uses a raw `rgba` shadow instead of `--ce-shadow-raised`.
- Fitment band on PDP sits in the host white column; it looks like a banner, not a product-system badge.
- Product images in the demo catalog are low-res placeholders — they drag perceived quality below the chrome.

---

## Interaction audit vs [23]

| Rule | Status |
|---|---|
| Never invent Fits | Pass — select / unknown / unmatched VIN |
| WCAG AA on CE widgets | Partial — G6 axe on widgets; false `aria-haspopup`; prompt not keyboard-documented |
| RTL logical properties | Pass on CE chrome; fail on host + catalog strings |
| Zero results offer a next step | Pass |
| Trade-speed VIN → cart | Fail — prompt + no decode-to-results sheet |
| Clear vehicle needs confirm (`FR-714` / `AC-23.3`) | Not present in chrome (API exists) |
| Reduced motion tokens | Pass in CSS |

---

## Recommended Figma file

Create **Check Engine — Storefront + Admin** with these pages:

1. **Foundations** — primitives, semantic tokens, type ramp EN/AR, 4.5:1 swatches, icon stroke 1.75.
2. **Components** — Button, Chip, Select, Search field, Fitment band (5 states), Mega, Modal/sheet, Product tile, Admin table.
3. **Storefront EN** — Home, Category, PDP (select / fits / unfit / unknown / unmatched VIN), Search (results + empty), Garage sheet, VIN disambiguation.
4. **Storefront AR** — same frames, `dir=rtl`, no mixed-script titles.
5. **Breakpoints** — 320 / 768 / 1280 / 1440 of home + PDP.
6. **Admin** — Dashboard (tasks, not links), Vehicle tree, Fitment queue, Import batch.
7. **This review** — FigJam: one sticker per finding, linked to a frame.

Drop the captures in `/opt/cursor/artifacts/figma-ui-ux-review/` onto page 7 as reference images.

---

## First design sprint status

Items 1–4 shipped in plugin `0.92.0` (UX.1–UX.8). Item 5 remains a product decision; do not start a theme package until that call is made.

1. Done — garage sheet replaces `window.prompt`; VIN masked; in-sheet remove confirm (`AC-23.3`).
2. Done — chip opens the sheet; rail `<select>` is visually hidden.
3. Done — plugin CSS hides host DefaultClean search when `.ce-rail` is present.
4. Done — Order Inspector deep-links `#ce-order-inspector`; uninstall sits below portals.
5. Done (UX.9, plugin `0.96.0`) — Check Engine theme package: dark shell, wordmark, shopper templates, self-hosted fonts, host menu removed. Lighthouse CWV (`AC-21.3`) remains G6.
