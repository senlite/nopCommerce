#!/usr/bin/env python3
"""Generate the Check Engine fitment accuracy corpus.

The corpus is the v1.0 release gate for fitment correctness (AC-35.3: the Must set
must pass at 100% on a release candidate). It encodes the evaluation semantics
required by FR-302 (four verdicts), FR-303/FR-304 (only published claims are
customer-visible), FR-307 (missing qualifier context must never force Fits) and
AC-026.1 / AC-15.5 (the engine fails closed and never fails open into Fits).

Cases are self-contained: each carries its own claims and evaluation context, so
the corpus runs against the evaluation service without a database.

Usage:
    python3 build_corpus.py
"""

from __future__ import annotations

import json
from pathlib import Path

OUTPUT = Path(__file__).with_name("fitment-corpus.json")

FITS_CONFIDENCE_THRESHOLD = 0.60

# Qualifier field -> (claim value, matching context value, conflicting context value)
QUALIFIERS = {
    "steeringSide": ("LHD", "LHD", "RHD"),
    "marketRegion": ("ECE", "ECE", "USDM"),
    "driveType": ("RWD", "RWD", "AWD"),
    "transmissionType": ("AT", "AT", "MT"),
}

MISMATCH_REASONS = {
    "steeringSide": "fitment.steering_side_mismatch",
    "marketRegion": "fitment.market_region_mismatch",
    "driveType": "fitment.drive_type_mismatch",
    "transmissionType": "fitment.transmission_type_mismatch",
}

MISSING_REASONS = {
    "steeringSide": "fitment.steering_side_required",
    "marketRegion": "fitment.market_region_required",
    "driveType": "fitment.drive_type_required",
    "transmissionType": "fitment.transmission_type_required",
    "productionYear": "fitment.production_year_required",
}

SAFETY_CLASSES = ["Standard", "Elevated", "SafetyCritical"]


def claim(
    status="Fits",
    confidence=0.95,
    published=True,
    active=True,
    safety="Standard",
    source="CuratorManual",
    qualifier=None,
):
    return {
        "status": status,
        "confidence": confidence,
        "isPublished": published,
        "isActive": active,
        "safetyClass": safety,
        "sourceKind": source,
        "qualifier": qualifier or {},
    }


def qualifier_state(claim_qualifier: dict, context: dict):
    """Classify a claim's qualifier against the context.

    Returns ("match", None), ("mismatch", reason) or ("indeterminate", reason).
    A claim is only usable when every stated qualifier is satisfied.
    """
    indeterminate_reason = None

    from_year = claim_qualifier.get("productionFromYear")
    to_year = claim_qualifier.get("productionToYear")
    if from_year is not None or to_year is not None:
        year = context.get("productionYear")
        if year is None:
            indeterminate_reason = indeterminate_reason or MISSING_REASONS["productionYear"]
        elif (from_year is not None and year < from_year) or (to_year is not None and year > to_year):
            return "mismatch", "fitment.production_year_out_of_range"

    for field in QUALIFIERS:
        required = claim_qualifier.get(field)
        if required is None:
            continue
        actual = context.get(field)
        if actual is None:
            indeterminate_reason = indeterminate_reason or MISSING_REASONS[field]
        elif actual != required:
            return "mismatch", MISMATCH_REASONS[field]

    if indeterminate_reason is not None:
        return "indeterminate", indeterminate_reason

    return "match", None


def expected_outcome(claims: list[dict], context: dict):
    """Reference implementation of the required evaluation semantics.

    Mirrors the algorithm in docs/15-fitment-engine.md so the corpus states the
    specified behaviour rather than whatever the engine happens to do.
    """
    visible = [c for c in claims if c["isPublished"] and c["isActive"]]
    if not visible:
        return {"outcome": "Unknown", "reasonCode": "fitment.no_published_claim"}

    states = [(c, *qualifier_state(c["qualifier"], context)) for c in visible]

    # Negative claims win over positives regardless of confidence, and a negative
    # claim whose context is unknown still counts (fail closed, AC-026.1).
    negatives = [
        (c, state, reason)
        for (c, state, reason) in states
        if c["status"] == "DoesNotFit" and state != "mismatch"
    ]
    if negatives:
        return {"outcome": "DoesNotFit", "reasonCode": "fitment.negative_claim"}

    positives = [c for (c, state, _) in states if c["status"] == "Fits" and state == "match"]
    if positives:
        best = max(c["confidence"] for c in positives)
        if best >= FITS_CONFIDENCE_THRESHOLD:
            return {"outcome": "Fits", "reasonCode": None}
        return {"outcome": "Unknown", "reasonCode": "fitment.insufficient_confidence"}

    indeterminate = [
        reason for (c, state, reason) in states if c["status"] == "Fits" and state == "indeterminate"
    ]
    if indeterminate:
        return {"outcome": "NeedsDisambiguation", "reasonCode": indeterminate[0]}

    mismatched = [
        reason for (c, state, reason) in states if c["status"] == "Fits" and state == "mismatch"
    ]
    if mismatched:
        return {"outcome": "DoesNotFit", "reasonCode": mismatched[0]}

    return {"outcome": "Unknown", "reasonCode": "fitment.no_published_claim"}


class CorpusBuilder:
    def __init__(self):
        self.cases: list[dict] = []
        self._seq = 0

    def add(self, name, description, claims, context, tier="Must", tags=None):
        self._seq += 1
        case = {
            "id": f"CE-FIT-{self._seq:04d}",
            "name": name,
            "tier": tier,
            "description": description,
            "tags": tags or [],
            "claims": claims,
            "context": context,
            "expected": expected_outcome(claims, context),
        }
        self.cases.append(case)
        return case


def build() -> dict:
    builder = CorpusBuilder()

    # --- Visibility: only published, active claims may be customer-visible (FR-303) ---
    builder.add(
        "no_claims_is_unknown",
        "A product with no fitment claim resolves to Unknown, never Fits.",
        [],
        {},
        tags=["fail-closed", "visibility"],
    )
    builder.add(
        "unpublished_claim_is_invisible",
        "An unpublished positive claim must not reach the customer.",
        [claim(published=False)],
        {},
        tags=["fail-closed", "visibility"],
    )
    builder.add(
        "inactive_claim_is_invisible",
        "A deactivated claim is retained but ignored during evaluation (FR-326).",
        [claim(active=False)],
        {},
        tags=["fail-closed", "visibility"],
    )
    builder.add(
        "rejected_claim_is_invisible",
        "A rejected claim never produces a positive verdict.",
        [claim(status="Rejected")],
        {},
        tags=["fail-closed", "visibility"],
    )
    builder.add(
        "unpublished_high_confidence_claim_is_invisible",
        "Confidence does not bypass the publication gate.",
        [claim(confidence=0.99, published=False)],
        {},
        tags=["fail-closed", "visibility"],
    )

    # --- Negative precedence: a negative claim outranks any positive (AC-026.1) ---
    for safety in SAFETY_CLASSES:
        builder.add(
            f"negative_beats_higher_confidence_positive_{safety.lower()}",
            "A published DoesNotFit claim must win even when a positive claim is more confident.",
            [
                claim(status="DoesNotFit", confidence=0.70, safety=safety),
                claim(status="Fits", confidence=0.99, safety=safety),
            ],
            {},
            tags=["fail-closed", "negative-precedence", safety.lower()],
        )
        builder.add(
            f"low_confidence_negative_still_wins_{safety.lower()}",
            "Even a low-confidence negative claim suppresses a near-certain positive.",
            [
                claim(status="DoesNotFit", confidence=0.30, safety=safety),
                claim(status="Fits", confidence=0.98, safety=safety),
            ],
            {},
            tags=["fail-closed", "negative-precedence", safety.lower()],
        )
        builder.add(
            f"negative_alone_{safety.lower()}",
            "A single negative claim resolves to DoesNotFit.",
            [claim(status="DoesNotFit", confidence=0.90, safety=safety)],
            {},
            tags=["negative-precedence", safety.lower()],
        )

    # --- Confidence banding around the Fits threshold ---
    for safety in SAFETY_CLASSES:
        for confidence, label in [
            (0.10, "very_low"),
            (0.45, "low"),
            (0.59, "just_below_threshold"),
            (0.60, "at_threshold"),
            (0.75, "above_threshold"),
            (0.99, "near_certain"),
        ]:
            builder.add(
                f"confidence_{label}_{safety.lower()}",
                f"A positive claim at confidence {confidence:.2f} must respect the Fits threshold.",
                [claim(confidence=confidence, safety=safety)],
                {},
                tags=["confidence", safety.lower()],
            )

    # --- Production year window ---
    for safety in SAFETY_CLASSES:
        window = {"productionFromYear": 2012, "productionToYear": 2018}
        builder.add(
            f"year_inside_window_{safety.lower()}",
            "A vehicle inside the claim's production window fits.",
            [claim(safety=safety, qualifier=window)],
            {"productionYear": 2015},
            tags=["qualifier", "production-year", safety.lower()],
        )
        builder.add(
            f"year_after_window_{safety.lower()}",
            "A vehicle built after the window must not be reported as fitting.",
            [claim(safety=safety, qualifier=window)],
            {"productionYear": 2020},
            tags=["qualifier", "production-year", "fail-closed", safety.lower()],
        )
        builder.add(
            f"year_before_window_{safety.lower()}",
            "A vehicle built before the window must not be reported as fitting.",
            [claim(safety=safety, qualifier=window)],
            {"productionYear": 2009},
            tags=["qualifier", "production-year", "fail-closed", safety.lower()],
        )
        builder.add(
            f"year_boundary_start_{safety.lower()}",
            "The first year of the window is inclusive.",
            [claim(safety=safety, qualifier=window)],
            {"productionYear": 2012},
            tags=["qualifier", "production-year", "boundary", safety.lower()],
        )
        builder.add(
            f"year_boundary_end_{safety.lower()}",
            "The last year of the window is inclusive.",
            [claim(safety=safety, qualifier=window)],
            {"productionYear": 2018},
            tags=["qualifier", "production-year", "boundary", safety.lower()],
        )
        builder.add(
            f"year_missing_context_{safety.lower()}",
            "An unknown build year must prompt for detail rather than assume a fit (FR-307).",
            [claim(safety=safety, qualifier=window)],
            {},
            tags=["qualifier", "production-year", "disambiguation", safety.lower()],
        )
        builder.add(
            f"year_open_ended_start_{safety.lower()}",
            "An open start bound only constrains the upper year.",
            [claim(safety=safety, qualifier={"productionToYear": 2016})],
            {"productionYear": 1998},
            tags=["qualifier", "production-year", safety.lower()],
        )
        builder.add(
            f"year_open_ended_end_{safety.lower()}",
            "An open end bound only constrains the lower year.",
            [claim(safety=safety, qualifier={"productionFromYear": 2016})],
            {"productionYear": 2030},
            tags=["qualifier", "production-year", safety.lower()],
        )

    # --- Every non-year qualifier: match, conflict and unknown context ---
    for field, (claim_value, matching, conflicting) in QUALIFIERS.items():
        for safety in SAFETY_CLASSES:
            builder.add(
                f"{field}_matches_{safety.lower()}",
                f"A claim qualified to {field}={claim_value} fits a matching vehicle.",
                [claim(safety=safety, qualifier={field: claim_value})],
                {field: matching},
                tags=["qualifier", field, safety.lower()],
            )
            builder.add(
                f"{field}_conflicts_{safety.lower()}",
                f"A claim qualified to {field}={claim_value} must not fit a {conflicting} vehicle.",
                [claim(safety=safety, qualifier={field: claim_value})],
                {field: conflicting},
                tags=["qualifier", field, "fail-closed", safety.lower()],
            )
            builder.add(
                f"{field}_unknown_context_{safety.lower()}",
                f"An unknown {field} must prompt for detail rather than assume a fit.",
                [claim(safety=safety, qualifier={field: claim_value})],
                {},
                tags=["qualifier", field, "disambiguation", safety.lower()],
            )
            builder.add(
                f"{field}_negative_claim_conflict_{safety.lower()}",
                f"A negative claim scoped to {field}={claim_value} does not apply to other vehicles.",
                [claim(status="DoesNotFit", safety=safety, qualifier={field: claim_value})],
                {field: conflicting},
                tags=["qualifier", field, "negative-precedence", safety.lower()],
            )
            builder.add(
                f"{field}_negative_claim_unknown_context_{safety.lower()}",
                f"A negative claim scoped to {field} still suppresses when {field} is unknown.",
                [claim(status="DoesNotFit", safety=safety, qualifier={field: claim_value})],
                {},
                tags=["qualifier", field, "fail-closed", safety.lower()],
            )

    # --- Combined qualifiers ---
    combined = {"steeringSide": "LHD", "marketRegion": "ECE", "productionFromYear": 2014}
    for safety in SAFETY_CLASSES:
        builder.add(
            f"combined_all_satisfied_{safety.lower()}",
            "Every stated qualifier is satisfied, so the claim applies.",
            [claim(safety=safety, qualifier=combined)],
            {"steeringSide": "LHD", "marketRegion": "ECE", "productionYear": 2019},
            tags=["qualifier", "combined", safety.lower()],
        )
        builder.add(
            f"combined_one_conflict_{safety.lower()}",
            "A single conflicting qualifier disqualifies the claim.",
            [claim(safety=safety, qualifier=combined)],
            {"steeringSide": "RHD", "marketRegion": "ECE", "productionYear": 2019},
            tags=["qualifier", "combined", "fail-closed", safety.lower()],
        )
        builder.add(
            f"combined_one_unknown_{safety.lower()}",
            "A single unknown qualifier makes the claim indeterminate.",
            [claim(safety=safety, qualifier=combined)],
            {"steeringSide": "LHD", "productionYear": 2019},
            tags=["qualifier", "combined", "disambiguation", safety.lower()],
        )
        builder.add(
            f"combined_conflict_outranks_unknown_{safety.lower()}",
            "A definite conflict is reported ahead of an unknown qualifier.",
            [claim(safety=safety, qualifier=combined)],
            {"steeringSide": "RHD", "productionYear": 2019},
            tags=["qualifier", "combined", "fail-closed", safety.lower()],
        )

    # --- Multiple positive claims ---
    for safety in SAFETY_CLASSES:
        builder.add(
            f"best_matching_positive_wins_{safety.lower()}",
            "The most confident applicable claim decides the verdict.",
            [
                claim(confidence=0.62, safety=safety),
                claim(confidence=0.97, safety=safety),
            ],
            {},
            tags=["multi-claim", safety.lower()],
        )
        builder.add(
            f"applicable_claim_preferred_over_conflicting_{safety.lower()}",
            "A claim that matches the vehicle is used even when a conflicting claim is more confident.",
            [
                claim(confidence=0.99, safety=safety, qualifier={"steeringSide": "RHD"}),
                claim(confidence=0.80, safety=safety, qualifier={"steeringSide": "LHD"}),
            ],
            {"steeringSide": "LHD"},
            tags=["multi-claim", "qualifier", safety.lower()],
        )
        builder.add(
            f"all_positives_conflict_{safety.lower()}",
            "When every positive claim conflicts, the part must not be shown as fitting.",
            [
                claim(confidence=0.99, safety=safety, qualifier={"steeringSide": "RHD"}),
                claim(confidence=0.95, safety=safety, qualifier={"marketRegion": "USDM"}),
            ],
            {"steeringSide": "LHD", "marketRegion": "ECE"},
            tags=["multi-claim", "fail-closed", safety.lower()],
        )
        builder.add(
            f"applicable_claim_preferred_over_indeterminate_{safety.lower()}",
            "A fully matching claim resolves the verdict even when another claim lacks context.",
            [
                claim(confidence=0.90, safety=safety, qualifier={"driveType": "AWD"}),
                claim(confidence=0.88, safety=safety),
            ],
            {},
            tags=["multi-claim", "disambiguation", safety.lower()],
        )
        builder.add(
            f"low_confidence_match_with_indeterminate_{safety.lower()}",
            "A matching but under-threshold claim still fails closed to Unknown.",
            [
                claim(confidence=0.20, safety=safety),
                claim(confidence=0.99, safety=safety, qualifier={"marketRegion": "GCC"}),
            ],
            {},
            tags=["multi-claim", "confidence", "fail-closed", safety.lower()],
        )

    # --- Provenance: AI-sourced claims are only visible once published by review ---
    for safety in SAFETY_CLASSES:
        builder.add(
            f"ai_claim_unpublished_is_invisible_{safety.lower()}",
            "An unreviewed AI inference must never reach the customer (INV-006).",
            [claim(confidence=0.99, published=False, source="AiInference", safety=safety)],
            {},
            tags=["provenance", "ai", "fail-closed", safety.lower()],
        )
        builder.add(
            f"reviewed_ai_claim_is_visible_{safety.lower()}",
            "An AI-sourced claim that passed human review is evaluated like any other claim.",
            [claim(confidence=0.92, published=True, source="AiInference", safety=safety)],
            {},
            tags=["provenance", "ai", safety.lower()],
        )
        for source in ["SupplierCatalog", "CustomerReport", "VinDecode", "ImportedFeed"]:
            builder.add(
                f"{source.lower()}_published_claim_{safety.lower()}",
                f"A published {source} claim participates in evaluation.",
                [claim(confidence=0.91, source=source, safety=safety)],
                {},
                tags=["provenance", safety.lower()],
            )

    # --- Negative claims constrained by a production window ---
    for safety in SAFETY_CLASSES:
        recall = {"productionFromYear": 2015, "productionToYear": 2017}
        builder.add(
            f"negative_window_applies_{safety.lower()}",
            "A negative claim scoped to a build window applies to vehicles inside it.",
            [
                claim(status="DoesNotFit", confidence=0.85, safety=safety, qualifier=recall),
                claim(status="Fits", confidence=0.97, safety=safety),
            ],
            {"productionYear": 2016},
            tags=["negative-precedence", "production-year", "fail-closed", safety.lower()],
        )
        builder.add(
            f"negative_window_does_not_apply_{safety.lower()}",
            "A negative claim scoped to a build window leaves other vehicles unaffected.",
            [
                claim(status="DoesNotFit", confidence=0.85, safety=safety, qualifier=recall),
                claim(status="Fits", confidence=0.97, safety=safety),
            ],
            {"productionYear": 2021},
            tags=["negative-precedence", "production-year", safety.lower()],
        )
        builder.add(
            f"negative_window_unknown_year_{safety.lower()}",
            "An unknown build year cannot clear a windowed negative claim.",
            [
                claim(status="DoesNotFit", confidence=0.85, safety=safety, qualifier=recall),
                claim(status="Fits", confidence=0.97, safety=safety),
            ],
            {},
            tags=["negative-precedence", "production-year", "fail-closed", safety.lower()],
        )

    # --- Qualified positives that sit below the confidence threshold ---
    for field, (claim_value, matching, _) in QUALIFIERS.items():
        for safety in SAFETY_CLASSES:
            builder.add(
                f"{field}_matches_but_low_confidence_{safety.lower()}",
                "A matching qualifier does not rescue a claim below the confidence threshold.",
                [claim(confidence=0.35, safety=safety, qualifier={field: claim_value})],
                {field: matching},
                tags=["qualifier", field, "confidence", "fail-closed", safety.lower()],
            )

    # --- Two-qualifier conflict matrix ---
    pair = {"steeringSide": "LHD", "driveType": "RWD"}
    pair_contexts = [
        ("both_match", {"steeringSide": "LHD", "driveType": "RWD"}),
        ("first_conflicts", {"steeringSide": "RHD", "driveType": "RWD"}),
        ("second_conflicts", {"steeringSide": "LHD", "driveType": "AWD"}),
        ("both_conflict", {"steeringSide": "RHD", "driveType": "AWD"}),
    ]
    for label, context in pair_contexts:
        for safety in SAFETY_CLASSES:
            builder.add(
                f"pair_{label}_{safety.lower()}",
                f"Two-qualifier claim evaluated where {label.replace('_', ' ')}.",
                [claim(confidence=0.93, safety=safety, qualifier=pair)],
                context,
                tags=["qualifier", "combined", safety.lower()],
            )

    # --- Positive and negative claims that are both qualified ---
    for safety in SAFETY_CLASSES:
        builder.add(
            f"scoped_negative_and_scoped_positive_disjoint_{safety.lower()}",
            "A negative claim for another market leaves a matching positive claim intact.",
            [
                claim(status="DoesNotFit", confidence=0.90, safety=safety, qualifier={"marketRegion": "USDM"}),
                claim(status="Fits", confidence=0.90, safety=safety, qualifier={"marketRegion": "ECE"}),
            ],
            {"marketRegion": "ECE"},
            tags=["multi-claim", "negative-precedence", safety.lower()],
        )
        builder.add(
            f"scoped_negative_and_scoped_positive_overlap_{safety.lower()}",
            "When both claims apply to the vehicle, the negative claim wins.",
            [
                claim(status="DoesNotFit", confidence=0.65, safety=safety, qualifier={"marketRegion": "ECE"}),
                claim(status="Fits", confidence=0.96, safety=safety, qualifier={"marketRegion": "ECE"}),
            ],
            {"marketRegion": "ECE"},
            tags=["multi-claim", "negative-precedence", "fail-closed", safety.lower()],
        )
        builder.add(
            f"scoped_negative_unknown_market_{safety.lower()}",
            "An unknown market cannot clear a market-scoped negative claim.",
            [
                claim(status="DoesNotFit", confidence=0.65, safety=safety, qualifier={"marketRegion": "USDM"}),
                claim(status="Fits", confidence=0.96, safety=safety),
            ],
            {},
            tags=["multi-claim", "negative-precedence", "fail-closed", safety.lower()],
        )

    # --- Repeated and high-volume claim sets ---
    for safety in SAFETY_CLASSES:
        builder.add(
            f"duplicate_identical_claims_{safety.lower()}",
            "Duplicated identical claims do not change the verdict.",
            [claim(confidence=0.88, safety=safety), claim(confidence=0.88, safety=safety)],
            {},
            tags=["multi-claim", safety.lower()],
        )
        builder.add(
            f"many_claims_single_applicable_{safety.lower()}",
            "One applicable claim among many inapplicable ones still resolves correctly.",
            [
                claim(confidence=0.99, safety=safety, qualifier={"steeringSide": "RHD"}),
                claim(confidence=0.98, safety=safety, qualifier={"marketRegion": "USDM"}),
                claim(confidence=0.97, safety=safety, qualifier={"driveType": "AWD"}),
                claim(confidence=0.72, safety=safety, qualifier={"steeringSide": "LHD"}),
            ],
            {"steeringSide": "LHD", "marketRegion": "ECE", "driveType": "RWD"},
            tags=["multi-claim", "qualifier", safety.lower()],
        )

    cases = builder.cases
    return {
        "version": "1.0",
        "description": (
            "Check Engine fitment accuracy corpus. The Must set must pass at 100% on a "
            "release candidate (AC-35.3)."
        ),
        "fitsConfidenceThreshold": FITS_CONFIDENCE_THRESHOLD,
        "caseCount": len(cases),
        "cases": cases,
    }


def main() -> None:
    corpus = build()
    OUTPUT.write_text(json.dumps(corpus, indent=2) + "\n", encoding="utf-8")

    outcomes: dict[str, int] = {}
    safety_cases = 0
    for case in corpus["cases"]:
        outcome = case["expected"]["outcome"]
        outcomes[outcome] = outcomes.get(outcome, 0) + 1
        if any(c["safetyClass"] == "SafetyCritical" for c in case["claims"]):
            safety_cases += 1

    print(f"cases written: {corpus['caseCount']}")
    print(f"safety-critical cases: {safety_cases}")
    for outcome, count in sorted(outcomes.items()):
        print(f"  {outcome}: {count}")


if __name__ == "__main__":
    main()
