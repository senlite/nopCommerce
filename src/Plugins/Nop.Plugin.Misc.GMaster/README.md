# GMaster Catalog Importer

`Misc.GMaster` is a standalone nopCommerce 4.70 plugin that replaces the active catalog with the
GMaster BMW-compatible parts catalog extracted from the supplier container packing lists:

- `CULU6339343_21-01-2026.xlsx`
- `PILU8022228_15-01-2026.xlsx`

Each workbook's `PICTURE` sheet carries an OE number, English product name, fitment description, RMB
cost price, and one **embedded product photo** per row. The bundled dataset holds **426 distinct
products** (repeated OE/description/price lines removed), **425 with a real supplier photo**.

## Regenerating the dataset

`tools/build_catalog.py` rebuilds `Content/gmaster-catalog.csv` and the `Content/parts` image folder
from the source workbooks. It is deterministic and idempotent:

```bash
python3 -m pip install openpyxl
python3 tools/build_catalog.py CULU6339343_21-01-2026.xlsx PILU8022228_15-01-2026.xlsx
```

Each product is classified by keyword into one of twelve categories:

| Category | Products |
|---|---:|
| Cooling system | 105 |
| Suspension | 67 |
| Fuel & air intake | 46 |
| Engine components | 45 |
| Interior trim | 36 |
| Sensors & electrical | 29 |
| Exterior & body | 26 |
| Steering | 19 |
| Engine & transmission mounts | 15 |
| Misc accessories | 14 |
| Braking | 13 |
| Driveline & hubs | 11 |

## Destructive installation contract

Installing the plugin:

1. Parses and validates the whole bundled CSV before changing the store.
2. Stages the complete GMaster replacement as unpublished, so a staging failure leaves the current
   storefront online.
3. Publishes the GMaster root category, twelve subcategories, and all staged products before destructive
   cleanup, so a cutover failure can overlap catalogs but cannot blank the storefront.
4. Clears current cart and wishlist rows so no session retains a now-deleted product.
5. Soft-deletes every previous active product and category through nopCommerce services.
6. Stores the EGP-converted supplier cost in `ProductCost` and the retail price in `Price`.
7. Sets EGP as the store's primary currency and unpublishes other currencies so the EGP amounts cannot
   be shown under a misleading currency symbol.
8. Attaches the real supplier photo per product, falling back to an original trademark-free category SVG
   only for rows without a photo.

Soft-delete is deliberate: it clears the visible catalog while preserving historical orders and
invoices that reference old product ids.

## Pricing

Supplier prices are in **RMB**. Cost is converted to EGP using a configurable rate (default `7.0`,
editable on the configuration page), then a retail margin is applied and rounded **up** to the next
EGP 10:

| EGP cost | Gross markup |
|---|---:|
| ≤ EGP 500 | 55% |
| EGP 501–1,000 | 45% |
| EGP 1,001–3,000 | 35% |
| EGP 3,001–10,000 | 28% |
| > EGP 10,000 | 22% |

## Reimport

The configuration page shows import statistics, the current RMB→EGP rate, and offers manual
replacement. Reimport requires the `ManagePlugins` **and** `ManageProducts` permissions and the exact
confirmation `REPLACE CATALOG`. Editing the rate and reimporting recalculates every price.

Uninstalling the plugin does **not** delete the imported catalog.
