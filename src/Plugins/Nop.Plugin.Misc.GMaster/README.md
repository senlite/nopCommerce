# GMaster Catalog Importer

`Misc.GMaster` is a standalone nopCommerce 4.70 plugin that replaces the active catalog during plugin
installation with the curated BMW-compatible parts data supplied in:

- `accessories.pdf` — interior/exterior accessories, switches, grilles, mirrors, lighting, and styling
- `fiber.pdf` — fender liners, underbody protection, arches, insulation, and bumpers

The bundled source contains **184 distinct saleable products** after removing repeated page/list
duplicates. Left/right, front/rear, colour, finish, and chassis variants remain separate products.

| Category | Products |
|---|---:|
| Mirrors | 38 |
| Body & underbody protection | 34 |
| Interior handles & trim | 27 |
| Lighting & lenses | 22 |
| Exterior grilles & trim | 20 |
| Electrical switches & controls | 17 |
| Climate & air vents | 9 |
| Spoilers & styling | 8 |
| Cupholders & storage | 6 |
| Steering wheels | 3 |

## Destructive installation contract

Installing the plugin:

1. Parses and validates the whole bundled CSV before changing the store.
2. Stages the complete GMaster replacement as unpublished, so a staging failure leaves the current
   storefront online.
3. Soft-deletes every previous active product and category through nopCommerce services.
4. Clears current cart and wishlist rows so no session retains a now-deleted product.
5. Publishes a GMaster root category, ten focused subcategories, and the staged products.
6. Stores supplier cost in `ProductCost` and calculated EGP selling price in `Price`.
7. Sets EGP as the store's primary currency and unpublishes other currencies so the EGP amounts cannot
   be shown under a misleading currency symbol.
8. Adds original, trademark-free SVG category illustrations (also reused as product placeholders).

Soft-delete is deliberate: it clears the visible catalog while preserving historical orders and
invoices that reference old product ids.

## Pricing

Selling prices are rounded **up** to the next EGP 10 after applying:

| Supplier cost | Gross markup |
|---|---:|
| ≤ EGP 500 | 55% |
| EGP 501–1,000 | 45% |
| EGP 1,001–3,000 | 35% |
| EGP 3,001–10,000 | 28% |
| > EGP 10,000 | 22% |

## Reimport

The configuration page shows import statistics and offers manual replacement. To prevent accidental
catalog loss, manual reimport requires typing the exact confirmation `REPLACE CATALOG`.

Uninstalling the plugin does **not** delete the imported catalog.
