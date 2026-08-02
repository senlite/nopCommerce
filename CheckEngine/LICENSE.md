# Check Engine™ End User Licence Agreement

**Twin Particles Commercial Software Licence**
**Version 1.0 — Effective for Check Engine 1.0.0 and later**

> **Notice:** This is a commercial software licence. Check Engine is **not** open-source software. This
> document is the licence under which the Check Engine software is distributed. It is separate from,
> and does not affect, the licence of the nopCommerce platform on which Check Engine operates.
>
> For the commercial framing of these terms — licence tiers, pricing, activation, entitlement
> enforcement, renewal, and the trademark compliance obligations that fall on the Licensee — see
> [43 Licensing](docs/43-licensing.md) and [44 Commercial Strategy](docs/44-commercial-strategy.md).

---

## Table of contents

- [1. Definitions](#1-definitions)
- [2. Grant of licence](#2-grant-of-licence)
- [3. Licence tiers and scope](#3-licence-tiers-and-scope)
- [4. Restrictions](#4-restrictions)
- [5. Source code provisions](#5-source-code-provisions)
- [6. Licence activation and validation](#6-licence-activation-and-validation)
- [7. Intellectual property](#7-intellectual-property)
- [8. Third-party components](#8-third-party-components)
- [9. Vehicle and part data](#9-vehicle-and-part-data)
- [10. Trademarks and nominative use](#10-trademarks-and-nominative-use)
- [11. Artificial intelligence features](#11-artificial-intelligence-features)
- [12. Data protection](#12-data-protection)
- [13. Support and maintenance](#13-support-and-maintenance)
- [14. Term and termination](#14-term-and-termination)
- [15. Warranty](#15-warranty)
- [16. Limitation of liability](#16-limitation-of-liability)
- [17. Indemnification](#17-indemnification)
- [18. Audit rights](#18-audit-rights)
- [19. Export control and sanctions](#19-export-control-and-sanctions)
- [20. General provisions](#20-general-provisions)
- [Appendix A: Documentation licence](#appendix-a-documentation-licence)
- [Appendix B: Third-party notices](#appendix-b-third-party-notices)

---

## Preamble

This End User Licence Agreement ("**Agreement**") is a binding legal contract between **Twin
Particles** ("**Licensor**", "we", "us") and the individual or legal entity acquiring a licence to the
Software ("**Licensee**", "you").

By downloading, installing, activating, copying, or otherwise using the Software, you accept this
Agreement in full. If you are accepting on behalf of an organisation, you represent that you have
authority to bind that organisation. If you do not accept these terms, do not install or use the
Software, and you may request a refund in accordance with Section 14.4.

---

## 1. Definitions

| Term | Definition |
|---|---|
| **Software** | The Check Engine™ automotive commerce platform for nopCommerce, comprising the `TwinParticles.CheckEngine` plugin, its bundled theme assets, database migrations, localisation resources, administrative interfaces, and any Updates supplied under this Agreement. |
| **Companion Plugins** | The separately distributed `TwinParticles.Payments.Paymob` and `TwinParticles.Shipping.Bosta` plugins, licensed under this Agreement when supplied by Licensor. |
| **Documentation** | The contents of the `docs/` directory and the Markdown files in the repository root, as governed by [Appendix A](#appendix-a-documentation-licence). |
| **Production Instance** | A deployment of the Software serving live commercial traffic to end customers. |
| **Non-Production Instance** | A development, staging, testing, training, disaster-recovery standby, or continuous-integration deployment that serves no live commercial traffic. |
| **Store** | A single nopCommerce store record, as represented by the platform's multi-store feature, operating under one primary domain. |
| **Licence Key** | The cryptographically signed entitlement token issued by Licensor that enables the Software and encodes its tier, term, and permitted Instance count. |
| **Subscription Term** | The period for which the Licence Key is valid, as stated on the applicable order. |
| **Update** | A patch or minor release supplied within the Subscription Term, such as 1.0.1 or 1.1.0. |
| **Upgrade** | A major release, such as 2.0.0, which may require a separate licence or renewal. |
| **Vehicle Data** | Vehicle hierarchy, VIN decoding rules, OEM part number, cross-reference, and fitment records. |
| **Licensee Data** | Data the Licensee inputs, imports, generates, or derives through use of the Software, including its own catalog, customer, and order data. |
| **AI Provider** | A third-party artificial intelligence service configured by the Licensee, such as OpenAI, Azure OpenAI, or Anthropic. |
| **Effective Date** | The date on which Licensor first issues a Licence Key to the Licensee. |

---

## 2. Grant of licence

### 2.1 Grant

Subject to your compliance with this Agreement and payment of all applicable fees, Licensor grants you
a **non-exclusive, non-transferable, non-sublicensable, revocable licence**, limited to the
Subscription Term, to:

1. **Install and execute** the Software on the number of Production Instances and Stores permitted by
   your licence tier, as set out in Section 3;
2. **Install and execute** the Software on Non-Production Instances in the quantities permitted by
   Section 3.3;
3. **Configure, extend, and customise** the Software for your own internal business purposes,
   including via the extension points documented in
   [09 Plugin Architecture](docs/09-plugin-architecture.md);
4. **Create and retain** backup copies as reasonably necessary for business continuity;
5. Where your tier includes source code, **modify the source code** for your own use, subject to
   Section 5.

### 2.2 Reservation of rights

All rights not expressly granted are reserved by Licensor. This Agreement grants a licence to use the
Software. **It does not transfer ownership** of the Software or of any intellectual property in it.

### 2.3 Nature of the grant

The Software is licensed, not sold. No title passes to the Licensee.

---

## 3. Licence tiers and scope

### 3.1 Tiers

Your rights are bounded by the tier stated on your order and encoded in your Licence Key.

| Tier | Production Instances | Stores per Instance | Source code | Marketplace module | Support |
|---|---|---|---|---|---|
| **Single Store** | 1 | 1 | No | No | Standard |
| **Multi Store** | 1 | Up to 5 | No | No | Standard |
| **Business** | 3 | Up to 10 each | Read-only | Yes | Priority |
| **Enterprise** | Unlimited within one legal entity | Unlimited | Modifiable | Yes | Dedicated |
| **OEM / Redistribution** | As negotiated | As negotiated | Modifiable | Yes | Negotiated |

Tier definitions, pricing, and the precise entitlement encoding are specified in
[43 Licensing](docs/43-licensing.md).

### 3.2 Per-entity scope

Unless your tier is OEM / Redistribution, the licence extends only to you and to your wholly owned
subsidiaries. It does not extend to affiliates under common control but not owned by you, to
franchisees, to joint ventures, or to clients.

### 3.3 Non-Production allowance

You may operate **three Non-Production Instances for each licensed Production Instance** at no
additional charge, provided that no Non-Production Instance serves live commercial traffic. Ephemeral
continuous-integration containers do not count against this allowance.

### 3.4 Agency and implementation partners

An agency, systems integrator, or consultancy deploying the Software on behalf of a client must ensure
that **the client is the named Licensee**. A partner may hold its own licence for development and
demonstration purposes, but may not use a single licence to serve multiple clients. Partner
arrangements are addressed in [44 Commercial Strategy](docs/44-commercial-strategy.md).

---

## 4. Restrictions

You shall not, and shall not permit any third party to:

### 4.1 Redistribution

1. Sell, resell, rent, lease, lend, sublicense, distribute, publish, or otherwise transfer the
   Software or any part of it to any third party;
2. Include the Software, in whole or in part, in any product, package, theme, template, or plugin
   offered to third parties, whether for a fee or free of charge;
3. Make the Software available on any public or private repository, marketplace, file-sharing service,
   or download site;
4. Provide the Software as a hosted, multi-tenant, or managed service to third parties, except under
   an OEM / Redistribution tier.

### 4.2 Reverse engineering and protection circumvention

1. Decompile, disassemble, deobfuscate, or otherwise attempt to derive the source code of any part of
   the Software supplied only in compiled form, except to the minimum extent that such an act is
   permitted by applicable law and cannot be contractually excluded;
2. Circumvent, disable, tamper with, remove, or otherwise interfere with the Licence Key validation
   mechanism, any entitlement check, any telemetry required for licence compliance, or any technical
   protection measure;
3. Generate, forge, share, publish, or traffic in Licence Keys;
4. Use the Software beyond the Instance, Store, or term limits encoded in your Licence Key.

### 4.3 Attribution and notices

1. Remove, obscure, alter, or falsify any copyright notice, trademark, licence notice, attribution, or
   proprietary marking contained in or displayed by the Software;
2. Misrepresent the origin or authorship of the Software.

### 4.4 Competitive and derivative use

1. Use the Software, its Documentation, or any knowledge derived from either for the purpose of
   designing, developing, or marketing a product that competes with the Software;
2. Extract, replicate, or reimplement the Vehicle Data schema, VIN decoding rules, OEM
   cross-reference structures, or fitment evaluation logic for use outside the Software;
3. Perform benchmarking, competitive analysis, or comparative testing for publication without
   Licensor's prior written consent.

### 4.5 Unlawful and abusive use

1. Use the Software in violation of any applicable law, regulation, or third-party right;
2. Use the Software to publish knowingly false vehicle compatibility information, counterfeit part
   listings, or misleading claims regarding the origin or authenticity of automotive parts;
3. Use the Software to infringe the trademark, design, or other rights of any vehicle manufacturer or
   parts supplier.

---

## 5. Source code provisions

### 5.1 Availability

Source code is supplied only with tiers indicated in Section 3.1. Business tier receives read-only
source access for audit, security review, and debugging. Enterprise and OEM tiers may modify.

### 5.2 Modification rights

Where modification is permitted, you may modify the source code for your own internal use only. All
such modifications:

1. Remain subject to every restriction in Section 4;
2. Are your sole responsibility, and are excluded from warranty under Section 15 and from support
   under Section 13;
3. Do not create any ownership interest in the underlying Software;
4. May not be distributed, in source or compiled form, to any third party.

### 5.3 Contributions

If you submit a modification, patch, bug fix, or suggestion to Licensor, you grant Licensor a
perpetual, worldwide, irrevocable, royalty-free, sublicensable licence to use, reproduce, modify,
distribute, and commercially exploit it without obligation or attribution to you. The submission
process is described in [CONTRIBUTING.md](CONTRIBUTING.md).

### 5.4 Escrow

Enterprise and OEM licensees may request source code escrow with a mutually agreed agent, at the
Licensee's cost, under a separate escrow agreement.

---

## 6. Licence activation and validation

### 6.1 Activation

The Software requires activation with a valid Licence Key. Activation transmits to Licensor only the
data necessary to validate and enforce entitlement:

| Transmitted | Purpose | Retention |
|---|---|---|
| Licence Key | Entitlement validation | Term plus 24 months |
| Instance fingerprint (salted hash of hostname and installation identifier) | Instance counting | Term plus 24 months |
| Store primary domain | Store counting | Term plus 24 months |
| Software version | Update eligibility, security notification | Term plus 24 months |
| nopCommerce and .NET version | Compatibility validation, support triage | Term plus 24 months |

### 6.2 What is not transmitted

Activation and validation **do not** transmit Licensee Data. No product, customer, order, pricing,
catalog, or traffic data leaves your infrastructure through the licensing mechanism. The complete data
flow, including the payload schema and the offline procedure, is specified in
[43 Licensing](docs/43-licensing.md) and [28 Security](docs/28-security.md).

### 6.3 Periodic revalidation

The Software revalidates entitlement periodically. If validation cannot complete — for example due to
a network fault — the Software continues to operate normally for a **grace period of 30 days**. The
Software will not disable a storefront, block orders, or interrupt commercial operation because of a
transient validation failure.

### 6.4 Air-gapped and offline deployment

Deployments without outbound internet access may use an offline activation file, issued on request and
valid for the Subscription Term. Offline activation is available at all tiers.

### 6.5 Consequence of expiry

On expiry or termination of the Subscription Term:

1. The storefront continues to serve customers, and no commercial operation is interrupted;
2. Administrative access to Check Engine configuration becomes read-only;
3. AI features, ERPNext synchronisation, and import processing cease;
4. Updates cease.

This design ensures that a lapsed licence never causes a revenue-affecting outage for the Licensee or
its customers.

---

## 7. Intellectual property

### 7.1 Ownership

The Software, the Documentation, and all intellectual property rights in them are and remain the
exclusive property of Twin Particles and its licensors. This includes without limitation the source
code, object code, architecture, database schema, algorithms, VIN decoding rules, fitment evaluation
logic, user interface designs, visual assets, prompt engineering, and the Check Engine™ name and
logo.

### 7.2 Feedback

Feedback you provide may be used by Licensor without restriction or compensation, subject to Section
5.3.

### 7.3 Licensee Data

**You retain all rights in Licensee Data.** Licensor claims no ownership of your catalog, customer,
order, or operational data. Licensor does not access Licensee Data except where you explicitly request
support assistance and grant access for that purpose.

---

## 8. Third-party components

The Software incorporates third-party components under their own licences. Those licences govern those
components, and nothing in this Agreement restricts rights granted to you under them.

| Component | Purpose | Licence |
|---|---|---|
| nopCommerce | Host platform | GNU GPL v3 with the nopCommerce Public Licence |
| .NET and ASP.NET Core | Runtime and web framework | MIT |
| FluentMigrator | Database migrations | Apache 2.0 |
| linq2db | Data access, as used by nopCommerce | MIT |
| FluentValidation | Model validation | Apache 2.0 |
| AutoMapper | Object mapping | MIT |
| Newtonsoft.Json / System.Text.Json | Serialisation | MIT |

A complete, versioned inventory with full licence texts and SPDX identifiers is maintained in
[Appendix B](#appendix-b-third-party-notices) and generated automatically in CI, as specified in
[33 CI-CD](docs/33-ci-cd.md).

### 8.1 nopCommerce platform relationship

Check Engine is a plugin that operates through nopCommerce's documented public extension interfaces
and does not modify nopCommerce core source. Twin Particles is an independent vendor and is not
affiliated with nopCommerce Ltd. You are responsible for holding a valid right to use nopCommerce.

---

## 9. Vehicle and part data

### 9.1 Curated data

Vehicle Data supplied with the Software is **curated by Licensor** and licensed to you under this
Agreement for use within the Software only. It is not licensed for extraction, resale, redistribution,
or use in any other system. Sourcing and curation methodology is documented in
[12 Vehicle Database](docs/12-vehicle-database.md).

### 9.2 No licensed third-party feed is included

The Software contains **no licensed third-party fitment database**. No TecDoc, ACES, PIES, or
equivalent commercial dataset is bundled. Where the Software provides connectors capable of importing
such data, it is **your responsibility** to hold a valid licence from the data owner and to comply
with its terms, including any restriction on publication or redistribution.

### 9.3 Accuracy and fitness — important limitation

Vehicle Data, VIN decoding results, OEM cross-references, fitment determinations, and AI-generated
compatibility inferences are provided **for reference and guidance only**. They are derived from
curated sources and automated enrichment, and they may be incomplete, outdated, or incorrect.

**You are solely responsible for verifying part applicability before sale or installation.** The
Software provides confidence scoring, provenance tracking, and human review workflows precisely
because automated determinations require verification; those controls are specified in
[15 Fitment Engine](docs/15-fitment-engine.md). Licensor accepts no liability for vehicle damage,
personal injury, financial loss, warranty invalidation, or regulatory consequence arising from
reliance on a fitment determination. This limitation is material to the pricing of the Software.

### 9.4 Safety-critical components

You acknowledge that certain automotive parts — including braking, steering, suspension, restraint,
and airbag components — are safety-critical, and that incorrect fitment may cause injury or death. You
shall implement independent professional verification for such components and shall not rely on the
Software as the sole determinant of applicability.

---

## 10. Trademarks and nominative use

### 10.1 Licensor's marks

"Check Engine", the Check Engine logo, and "Twin Particles" are trademarks of Twin Particles. This
Agreement grants no right to use them, except that you may factually state that your store is powered
by Check Engine.

### 10.2 Manufacturer marks — your obligation

The Software enables you to publish a catalog referencing vehicle manufacturer names, model
designations, and OEM part numbers. Such references are permissible in most jurisdictions under the
doctrine of **nominative fair use** — using a mark to identify the thing itself — but the doctrine has
limits, and **compliance is your responsibility as the publisher**.

You shall, at minimum:

1. Use manufacturer names and part numbers only as necessary to identify a vehicle or a part;
2. Not use manufacturer logos, wordmark styling, or trade dress without authorisation;
3. Not imply affiliation with, endorsement by, or sponsorship by any manufacturer;
4. Clearly identify aftermarket parts as aftermarket, and not represent them as genuine;
5. Display a disclaimer of affiliation on catalog pages that reference manufacturer marks.

The Software includes configurable disclaimer components and defaults them to enabled. Required
wording, placement rules, and jurisdictional guidance are specified in
[43 Licensing](docs/43-licensing.md).

### 10.3 No legal advice

Section 10.2 is a description of product behaviour and a statement of contractual allocation of
responsibility. **It is not legal advice.** Obtain advice from qualified counsel in each jurisdiction
in which you operate.

---

## 11. Artificial intelligence features

### 11.1 Provider relationship

AI features require you to configure and pay for your own AI Provider account. Licensor is not a party
to that relationship. Your use of an AI Provider is governed by that provider's terms.

### 11.2 Data sent to providers

When AI features are enabled, the Software transmits to your configured AI Provider the data necessary
to perform the requested operation — product text, specifications, vehicle context, or a search query.
Every AI feature is **individually configurable and disabled by default**, and the Software presents an
explicit disclosure of the data each feature transmits before that feature is enabled. The complete
per-feature data inventory is in [17 AI Architecture](docs/17-ai-architecture.md).

### 11.3 Generated content

Content generated by AI features — descriptions, specifications, translations, SEO metadata, and
compatibility inferences — is **your content and your responsibility**. Licensor makes no
representation as to its accuracy, originality, non-infringement, or fitness. The Software routes
AI-generated content through a review workflow before publication, and Licensor recommends that you
not disable that workflow. See [25 AI Content Pipeline](docs/25-ai-content-pipeline.md).

### 11.4 No AI training on Licensee Data

Licensor does not use Licensee Data to train any model. Licensor does not aggregate Licensee catalog
data across customers.

---

## 12. Data protection

### 12.1 Roles

For personal data processed within your deployment, **you are the controller** and Licensor is not a
processor, because the Software runs on your infrastructure and Licensor has no access to Licensee
Data. For the limited licence-validation data described in Section 6.1, Licensor is a controller for
the sole purpose of entitlement enforcement.

### 12.2 Compliance features

The Software provides data subject access, portability, rectification, erasure, consent management,
retention policy enforcement, and audit logging to support your obligations under GDPR and comparable
regimes. These are specified in [28 Security](docs/28-security.md).

### 12.3 Your obligations

You are responsible for lawful basis, notices, consent, cross-border transfer mechanisms, breach
notification, and records of processing in your deployment.

---

## 13. Support and maintenance

### 13.1 Included

An active Subscription Term includes Updates, security patches, documentation access, and support at
the level of your tier.

| Tier | Channel | First response target | Coverage |
|---|---|---|---|
| Single Store | Email, ticket portal | 2 business days | Business hours |
| Multi Store | Email, ticket portal | 1 business day | Business hours |
| Business | Priority ticket, scheduled calls | 8 business hours | Extended hours |
| Enterprise | Dedicated contact, direct escalation | 4 hours, 24×7 for severity 1 | 24×7 for severity 1 |

Severity definitions, escalation paths, and the maintenance commitment are in
[43 Licensing](docs/43-licensing.md).

### 13.2 Excluded

Support excludes: custom development; modified source code; third-party plugin conflicts not caused by
the Software; nopCommerce platform defects; AI Provider outages; server administration; data entry and
catalog curation; and training beyond the supplied Documentation.

### 13.3 Platform version support

Licensor supports the Software on the nopCommerce versions listed in the support matrix in
[README.md](README.md). Where nopCommerce releases a new major version, Licensor will publish a
compatible Check Engine release or a documented migration path.

---

## 14. Term and termination

### 14.1 Term

This Agreement begins on the Effective Date and continues for the Subscription Term, renewing as
stated on your order until terminated.

### 14.2 Termination by you

You may terminate at any time by ceasing use and destroying all copies. Fees already paid are
non-refundable except under Section 14.4.

### 14.3 Termination by Licensor

Licensor may terminate immediately on written notice if you: materially breach Section 4; fail to pay
within 30 days of written notice; circumvent licence validation; or become insolvent or subject to
liquidation.

### 14.4 Refund

A full refund is available within **30 days** of initial purchase if the Software does not
substantially conform to its Documentation and Licensor cannot remedy the non-conformity within a
reasonable period after written notice.

### 14.5 Effect of termination

On termination you shall cease all use and destroy all copies within 30 days, and certify destruction
on request. Sections 4, 7, 9.3, 9.4, 10, 15, 16, 17, 18, and 20 survive termination.

### 14.6 Data retrieval

Licensee Data resides in your database throughout. Termination does not impair your access to it.
Uninstalling the Software removes its schema; export your data before uninstalling. The uninstall
behaviour and the export procedure are specified in
[09 Plugin Architecture](docs/09-plugin-architecture.md).

---

## 15. Warranty

### 15.1 Limited warranty

Licensor warrants that for **90 days** from the Effective Date the Software will perform substantially
in accordance with its Documentation. Your exclusive remedy for breach is, at Licensor's option,
repair, replacement, or refund of fees paid for the non-conforming Software.

### 15.2 Disclaimer

**EXCEPT AS EXPRESSLY PROVIDED IN SECTION 15.1, THE SOFTWARE IS PROVIDED "AS IS" AND "AS AVAILABLE"
WITHOUT WARRANTY OF ANY KIND. TO THE MAXIMUM EXTENT PERMITTED BY LAW, LICENSOR DISCLAIMS ALL
WARRANTIES, EXPRESS, IMPLIED, STATUTORY, OR OTHERWISE, INCLUDING WITHOUT LIMITATION IMPLIED WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, TITLE, NON-INFRINGEMENT, AND ANY WARRANTY ARISING
FROM COURSE OF DEALING OR USAGE OF TRADE.**

**LICENSOR DOES NOT WARRANT THAT THE SOFTWARE WILL BE UNINTERRUPTED OR ERROR-FREE, THAT DEFECTS WILL
BE CORRECTED, THAT THE SOFTWARE IS FREE OF HARMFUL COMPONENTS, OR THAT VEHICLE DATA, VIN DECODING
RESULTS, OEM CROSS-REFERENCES, FITMENT DETERMINATIONS, OR AI-GENERATED CONTENT ARE ACCURATE, COMPLETE,
CURRENT, OR FIT FOR ANY PURPOSE. SECTIONS 9.3 AND 9.4 APPLY.**

### 15.3 Statutory rights

Some jurisdictions do not permit exclusion of certain warranties. Where such law applies, the
exclusions in Section 15.2 apply to the maximum extent permitted, and nothing in this Agreement limits
non-excludable consumer rights.

---

## 16. Limitation of liability

### 16.1 Exclusion of indirect damages

**TO THE MAXIMUM EXTENT PERMITTED BY LAW, LICENSOR SHALL NOT BE LIABLE FOR ANY INDIRECT, INCIDENTAL,
SPECIAL, CONSEQUENTIAL, EXEMPLARY, OR PUNITIVE DAMAGES, OR FOR ANY LOSS OF PROFIT, REVENUE, GOODWILL,
BUSINESS OPPORTUNITY, ANTICIPATED SAVINGS, OR DATA, ARISING OUT OF OR RELATING TO THIS AGREEMENT OR THE
SOFTWARE, WHETHER IN CONTRACT, TORT, NEGLIGENCE, STRICT LIABILITY, OR OTHERWISE, AND WHETHER OR NOT
LICENSOR WAS ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.**

### 16.2 Liability cap

**LICENSOR'S TOTAL AGGREGATE LIABILITY ARISING OUT OF OR RELATING TO THIS AGREEMENT SHALL NOT EXCEED
THE TOTAL FEES ACTUALLY PAID BY YOU TO LICENSOR FOR THE SOFTWARE IN THE TWELVE MONTHS IMMEDIATELY
PRECEDING THE EVENT GIVING RISE TO THE CLAIM.**

### 16.3 Fitment and safety exclusion

**WITHOUT LIMITING SECTIONS 16.1 AND 16.2, LICENSOR SHALL HAVE NO LIABILITY WHATSOEVER FOR ANY CLAIM
ARISING FROM AN INCORRECT VEHICLE COMPATIBILITY OR FITMENT DETERMINATION, INCLUDING CLAIMS FOR VEHICLE
DAMAGE, PROPERTY DAMAGE, PERSONAL INJURY, DEATH, PRODUCT RECALL, WARRANTY INVALIDATION, REGULATORY
PENALTY, OR CONSUMER CLAIM. SECTIONS 9.3 AND 9.4 APPLY.**

### 16.4 Exceptions to the cap

The limitations in Sections 16.1 and 16.2 do not apply to: Licensor's liability for death or personal
injury caused by its own negligence; fraud or fraudulent misrepresentation; wilful misconduct; or any
liability that cannot be limited or excluded under applicable law.

### 16.5 Allocation of risk

You acknowledge that the limitations in this Section reflect a deliberate allocation of risk, that they
are a material inducement to Licensor to supply the Software at the stated fees, and that they apply
even if a limited remedy fails of its essential purpose.

---

## 17. Indemnification

### 17.1 By Licensor

Licensor shall defend you against any third-party claim alleging that the Software, as supplied and
used in accordance with this Agreement, infringes a copyright, trademark, or trade secret, and shall
pay damages finally awarded, provided you promptly notify Licensor, grant sole control of the defence,
and cooperate reasonably.

Licensor's obligation does not apply to a claim arising from: modification of the Software by anyone
other than Licensor; combination with software, data, or hardware not supplied by Licensor; use in
breach of this Agreement; Licensee Data or imported third-party data; use of manufacturer trademarks
by you; continued use after Licensor supplies a non-infringing alternative; or Vehicle Data you have
created, modified, or imported.

If a claim arises, Licensor may at its option procure the right to continue use, modify the Software to
be non-infringing, or terminate the licence and refund fees for the unexpired Subscription Term. **This
is your sole and exclusive remedy for infringement.**

### 17.2 By Licensee

You shall indemnify, defend, and hold harmless Licensor against any third-party claim arising from:
your use of the Software in breach of this Agreement or of applicable law; Licensee Data or data you
import; your use of vehicle manufacturer trademarks; your publication of compatibility, fitment, or
part authenticity information; AI-generated content you publish; your modifications to the Software;
and any claim by your customers relating to parts sold through your store.

---

## 18. Audit rights

Licensor may, on **30 days' written notice**, not more than once in any twelve-month period, and during
normal business hours, verify your compliance with the Instance and Store limits of your tier. An
audit shall be conducted so as to minimise disruption, shall be limited to entitlement compliance, and
shall not extend to Licensee Data. Where an audit reveals under-licensing, you shall pay the shortfall
plus the cost of the audit within 30 days.

---

## 19. Export control and sanctions

You represent that you are not located in, organised under the laws of, or ordinarily resident in any
jurisdiction subject to comprehensive trade sanctions, and that you are not a person listed on any
applicable restricted-party list. You shall comply with all applicable export control and sanctions
laws and shall not export, re-export, or transfer the Software in violation of them.

---

## 20. General provisions

### 20.1 Governing law and jurisdiction

This Agreement is governed by the laws of the jurisdiction of Licensor's registered establishment,
excluding its conflict-of-laws rules and excluding the United Nations Convention on Contracts for the
International Sale of Goods. The courts of that jurisdiction have exclusive jurisdiction, save that
either party may seek injunctive relief in any competent court to protect its intellectual property.

The specific governing jurisdiction is stated on the order form and in the executed licence
certificate.

### 20.2 Dispute resolution

The parties shall first attempt good-faith resolution through senior representatives for 30 days
before commencing proceedings. This does not prevent either party seeking urgent injunctive relief.

### 20.3 Assignment

You may not assign this Agreement without Licensor's prior written consent, except to a successor of
your entire business by merger or acquisition, on written notice, provided the successor accepts these
terms and is not a competitor of Licensor. Licensor may assign to an affiliate or to a successor of its
business.

### 20.4 Entire agreement

This Agreement, together with the applicable order form, is the entire agreement between the parties
and supersedes all prior proposals, representations, and understandings. Any purchase order term that
conflicts with this Agreement is void.

### 20.5 Amendment

Licensor may amend this Agreement for future versions. Amendments do not apply retroactively to a
version already licensed to you. Continued use of a new version after notice constitutes acceptance of
the terms accompanying that version.

### 20.6 Severability

If any provision is held unenforceable, it shall be modified to the minimum extent necessary to be
enforceable, and the remaining provisions continue in full force.

### 20.7 Waiver

No failure or delay in exercising a right operates as a waiver. A waiver is effective only if in
writing and signed by the waiving party.

### 20.8 Force majeure

Neither party is liable for failure to perform, other than an obligation to pay, caused by an event
beyond its reasonable control, including natural disaster, war, civil unrest, epidemic, governmental
action, failure of a public utility or telecommunications network, or failure of a third-party cloud or
AI provider.

### 20.9 Notices

Notices shall be in writing and sent to the addresses stated on the order form, and are deemed given
on receipt, or on the second business day after dispatch by recorded delivery.

### 20.10 Relationship

The parties are independent contractors. Nothing creates a partnership, joint venture, agency, or
employment relationship.

### 20.11 Third-party beneficiaries

There are no third-party beneficiaries to this Agreement.

### 20.12 Language

The English text of this Agreement governs. Any translation is provided for convenience only.

### 20.13 Headings

Headings are for convenience and do not affect interpretation.

---

## Appendix A: Documentation licence

The Documentation in this repository — the contents of `docs/` and the Markdown files in the root — is
**confidential and proprietary to Twin Particles**, and is licensed to you solely for the purpose of
implementing, operating, and maintaining the Software.

**You may:** read, print, and internally distribute the Documentation to your employees and
contractors bound by confidentiality obligations; quote brief excerpts in internal technical
documentation with attribution; and create internal derivative material for training your own staff.

**You may not:** publish the Documentation or any substantial part of it publicly; distribute it to
third parties other than as permitted above; use it to develop a competing product, as provided in
Section 4.4; or remove proprietary notices from it.

The Documentation is provided without warranty as to accuracy or completeness, and is subject to change
without notice. Where the Documentation and the Software behaviour conflict, the Software behaviour
governs, and you should report the discrepancy as a defect.

---

## Appendix B: Third-party notices

The Software incorporates the third-party components listed in Section 8, each under its own licence.
Full licence texts, versions, and SPDX identifiers for every direct and transitive dependency are
published in the `THIRD-PARTY-NOTICES.md` file generated with each release build. The generation
process, the licence-compatibility policy, and the prohibited-licence list are specified in
[33 CI-CD](docs/33-ci-cd.md).

Licensor's policy is to incorporate only components under permissive licences — MIT, Apache 2.0, BSD,
or equivalent — with the exception of the nopCommerce platform itself, which the Software extends
through documented public interfaces without modification or static linkage of GPL-licensed code into
proprietary assemblies. The legal analysis supporting this position, and the boundary that must be
maintained to preserve it, is documented in [43 Licensing](docs/43-licensing.md).

---

**Copyright © Twin Particles. All rights reserved.**

Check Engine™ is a trademark of Twin Particles. nopCommerce is a trademark of nopCommerce Ltd. All
other trademarks referenced are the property of their respective owners and are used nominatively for
identification purposes only. Twin Particles is not affiliated with, endorsed by, or sponsored by any
vehicle manufacturer.
