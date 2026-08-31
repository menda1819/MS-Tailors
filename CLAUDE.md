# CLAUDE.md — MS Tailors

Working agreement for this repository. Read this before writing code.

---

## 1. The business

**MS Tailors** — a tailoring business in Panadura, Sri Lanka.

| | |
|---|---|
| Address | No. 28 Station Road, Panadura, Sri Lanka |
| Tagline | *Perfect Fit. Timeless Style.* |
| Secondary line | *Tailored to You. Crafted with Care.* |
| Locale | Sri Lanka · currency **LKR** · English (Sinhala copy may be added later) |

The site has **two first-class sides**. Neither is a sub-page of the other.

### 1a. Tailoring services — the parent business
Custom suits, wedding suits, business suits, custom shirts, custom trousers,
blazers, school / office / corporate uniforms, kids' formal wear, suit & coat
rental, and alterations.

**Goal: inquiries, not transactions.** Every service page drives toward a
WhatsApp message, a phone call, or a measurement-appointment request. There is
no checkout on this side and no online payment.

### 1b. Multi-brand online shop
MS Tailors is the **parent platform**. Independent clothing brands sell
ready-made products through it.

> **Brand names do not exist yet.** Brands are created at runtime by an admin.
> **Never hardcode a brand name, slug, logo, colour, or count anywhere** — not in
> components, not in nav, not in seed data, not in tests, not in copy. Any UI
> that lists brands renders from the API and must handle the zero-brand case.

Each brand gets its own identity, hero image, logo, story, product collection,
and SEO-friendly page at `/brands/[slug]`.

### The mental model a visitor must leave with
> "I can get something **custom-made by MS Tailors**, **and** buy **ready-made
> products from different brands**, on the same site."

The home page and the global nav must make both paths obvious above the fold.

### Decisions taken in Phase 0 (do not re-litigate without asking)
- **Payments:** PayHere (Sri Lankan LKR gateway) + Cash on Delivery. Written
  behind an `IPaymentProvider` abstraction so a second gateway is additive.
- **Brand management:** central admin only. MS Tailors staff create brands and
  products. **But** every brand-owned entity carries `brandId` and the role enum
  reserves `BrandOwner`, so vendor dashboards later are a feature, not a rewrite.
- **Checkout:** guest checkout, with an optional customer account for order
  history. Orders are keyed by email + phone, not by a required user id.
- **Images:** Cloudinary. Mongo stores the `publicId` and derived URLs only —
  never binary.

---

## 2. Stack — fixed, do not substitute

| Layer | Choice |
|---|---|
| Frontend | Next.js (App Router) + TypeScript + Tailwind CSS |
| Backend | .NET 8 Web API (LTS, supported to Nov 2026) + C# |
| Database | MongoDB Atlas via the official `MongoDB.Driver` |
| Auth | ASP.NET Core JWT bearer + rotating refresh tokens |
| Media | Cloudinary |
| Payments | PayHere + COD |
| Repo | monorepo — https://github.com/IT22350732/MS-Tailors.git |

**No ORM.** `MongoDB.Driver` directly, behind repository interfaces.
**No `dotnet ef`, no migrations.** Index creation is code at startup.

> **Ask before adding any dependency that is not obviously required.** "Obviously
> required" means the stack above cannot do the job. A helper library that saves
> twenty lines is not obviously required.

---

## 3. Repository layout

```
MS-Tailors/
├─ frontend/                      # Next.js App Router app
├─ backend/
│  ├─ MSTailors.Api/              # controllers, DI wiring, middleware, Program.cs
│  ├─ MSTailors.Application/      # services, DTOs, validation, interfaces
│  ├─ MSTailors.Domain/           # entities, enums, value objects
│  ├─ MSTailors.Infrastructure/   # Mongo context, repositories, Cloudinary, PayHere
│  └─ MSTailors.Tests/
├─ docs/
├─ .gitignore
├─ README.md
└─ CLAUDE.md
```

### Dependency rule (enforced by project references — check before adding one)

```
Api  ──▶ Application ──▶ Domain
 └──▶ Infrastructure ──▶ Application ──▶ Domain
```

- **Domain** references nothing. No Mongo attributes, no ASP.NET types, no NuGet
  beyond the BCL.
- **Application** defines the interfaces (`IProductRepository`, `IPaymentProvider`,
  `IMediaStore`). It never references `MongoDB.Driver`.
- **Infrastructure** implements them. It is the only project that knows Mongo,
  Cloudinary, or PayHere exist.
- **Api** is thin: bind, authorise, delegate to a service, map to a response.
  No business logic in controllers.

---

## 4. Conventions

### C#
- Nullable enabled, `ImplicitUsings` enabled, warnings as errors in CI.
- `PascalCase` types/methods/properties, `_camelCase` private fields,
  `camelCase` locals and parameters.
- One public type per file; file name matches the type.
- Async everywhere for I/O; suffix `Async`; always accept and pass
  `CancellationToken`.
- Entities are classes with `string Id` (Mongo ObjectId as string at the
  boundary), deriving from `Entity` (`Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`).
- Entities carry **explicit `[BsonElement]` attributes**. Field naming is
  declared per property, not inferred — the convention pack in
  `MongoConventions` is only a safety net for extra-element tolerance and string
  enums.
- **Never expose an entity from a controller.** Request/response DTOs only,
  named `CreateProductRequest`, `ProductResponse`, `ProductListItemResponse`.
- Validation: FluentValidation in Application, wired to the pipeline so invalid
  input returns `400` + RFC 7807 `ProblemDetails` before a service is reached.
- Errors: `ProblemDetails` for everything. No bare 500s, no leaked stack traces.

### Mongo
- Collection names: lowercase plural — `brands`, `products`, `orders`.
- Field names: `camelCase` via a global convention pack, not per-property attributes.
- Money is `decimal` in C#, `Decimal128` in BSON, via
  `[BsonRepresentation(BsonType.Decimal128)]`. Never `double`.
- Every collection has `createdAt` / `updatedAt` (UTC).
- **Soft delete is universal.** Every document has `isDeleted`, and every
  repository read path filters it out automatically. `HardDeleteAsync` exists for
  test teardown and admin repair only — never call it from a request path.
- Indexes are declared once in `MongoIndexInitializer` and created at startup.

### TypeScript / Next.js
- App Router. **Server Components by default** — add `"use client"` only when a
  component needs state, effects, or event handlers, and push it to the leaf.
- `PascalCase` components, `camelCase` functions/vars, `kebab-case` file and
  route-segment names.
- `frontend/src/` layout: `app/`, `components/ui/`, `components/<feature>/`,
  `lib/`, `types/`.
- One typed API client in `lib/api/`. Components never call `fetch` directly.
- Types mirroring API DTOs live in `types/` and are the single source of truth.
- No `any`. No non-null `!` to silence the compiler.
- Public pages use SSG/ISR where content is stable, SSR where it is not.

### Tailwind
- **Tailwind v4** — configuration is CSS-first. There is no `tailwind.config.ts`;
  design tokens are declared in `src/app/globals.css` under `@theme`.
  **No raw hex in components.**
- No arbitrary values (`w-[437px]`) unless there is a written reason.
- Compose repeated patterns into components, not `@apply` soup.

### Git
- Branches: `main` (production), `dev` (integration), `feat/…`, `fix/…`, `chore/…`.
- Conventional Commits: `feat(shop): add brand listing page`.
- Work merges into `dev`; `dev` merges into `main`.

---

## 5. Secrets policy — hard rules

- **Development:** .NET user secrets (`dotnet user-secrets`). **Production:**
  environment variables.
- **Frontend:** `.env.local`. Only `NEXT_PUBLIC_*` values may reach the browser.
  Anything without that prefix must never be read from a Client Component.
- `.env`, `.env.local`, `appsettings.Development.json`, and `secrets.json` are
  gitignored. **Never commit a connection string or key — not even in an example
  file.**
- `.env.example` and `appsettings.Example.json` carry **placeholders only**.
- **If a real credential is ever found in a git-tracked file: stop, do not
  commit, and tell the user immediately.**

### Config keys

Backend (`__` is the .NET nesting separator):

```
MongoDb__ConnectionString      MongoDb__DatabaseName          # mstailors
Jwt__Key                       Jwt__Issuer                    Jwt__Audience
Jwt__AccessTokenMinutes        Jwt__RefreshTokenDays
Cors__AllowedOrigins
Cloudinary__CloudName          Cloudinary__ApiKey             Cloudinary__ApiSecret
PayHere__MerchantId            PayHere__MerchantSecret        PayHere__Sandbox
```

Frontend (`.env.local`):

```
NEXT_PUBLIC_API_BASE_URL
NEXT_PUBLIC_WHATSAPP_NUMBER
NEXT_PUBLIC_SITE_URL
```

---

## 6. Design direction

Premium, elegant, modern, restrained. **A fashion house, not a small-business
template.** When a choice is between "more" and "less", choose less.

### Palette — deep neutrals, one restrained accent

| Token | Value | Use |
|---|---|---|
| `--ink` | `#14151A` | Primary text, dark sections |
| `--charcoal` | `#2A2C33` | Secondary surfaces |
| `--sand` | `#C9BBA8` | Warm neutral, dividers, muted type |
| `--bone` | `#F6F3EE` | Off-white page ground |
| `--brass` | `#A67A3D` | **The single accent.** Links, focus, key CTA only |

Brass is an accent, not a brand fill. If a screen has more than a few brass
elements, it is wrong.

### Type — two typefaces, no more
- **Display:** a high-contrast serif for headings and the tagline.
- **Text:** a neutral grotesque for body, UI, and labels.
- Loaded via `next/font` with `display: swap` and a real fallback stack.

### Layout & motion
- Generous whitespace. A wide, calm grid; large photography that is allowed to breathe.
- Subtle motion only — short, eased, opacity and small translations.
  **Respect `prefers-reduced-motion` on every animation.**
- Banned: stock-template gradients, drop-shadow stacks, clutter, a third typeface.

### Mobile
Designed deliberately, not a squeezed desktop layout. Decide what the phone
shows *first* — tap targets ≥ 44px, thumb-reachable primary actions, no
horizontal scroll on the body.

### Accessibility — not optional
- Every interactive element is keyboard reachable with a **visible focus state**
  (never `outline: none` without a replacement).
- Correct semantics and ARIA; a `<div>` with an `onClick` is a bug.
- Text contrast ≥ 4.5:1 (≥ 3:1 for large text). Brass on bone is checked, not assumed.
- Images carry meaningful `alt`; decorative images carry `alt=""`.

---

## 7. Non-negotiables (checked at review)

- Mobile-first responsive: phone → tablet → laptop → desktop.
- **Every public page is SSR or SSG.** No client-only data fetch for indexable content.
- Per-page metadata, one `<h1>`, semantic heading order, `sitemap.xml`,
  `robots.txt`, JSON-LD (`LocalBusiness` for the business, `Product` for products).
- `next/image` everywhere, lazy below the fold, explicit dimensions, no layout shift.
- **All API input validated server-side.** Never trust prices, totals, stock, or
  discounts from the browser — **recompute every total server-side from the
  database** at order time.
- A floating WhatsApp button on every page that does not obstruct content and is
  reachable by keyboard.

---

## 8. Commands

Filled in as the projects are scaffolded — they do not exist yet at Phase 0.

Frontend runs on **http://localhost:3000**, the API on **http://localhost:5001**
(Swagger at `/swagger`).

```bash
# Frontend (frontend/)
npm install
npm run dev              # dev server        -> http://localhost:3000
npm run build            # production build
npm run lint             # eslint
npm run typecheck        # next typegen && tsc --noEmit

# Backend (backend/)
dotnet restore
dotnet build MSTailors.sln
dotnet run --project MSTailors.Api    # -> http://localhost:5001
dotnet test MSTailors.sln
dotnet format
```

> `npm run typecheck` runs `next typegen` first on purpose. Next 16 generates the
> `PageProps` / `LayoutProps` global types into `.next/types`, so a bare
> `tsc --noEmit` fails on a clean checkout.

Setting a secret (run from `backend/MSTailors.Api`, output suppressed so the
value is not echoed into a terminal log):

```bash
dotnet user-secrets set "MongoDb__ConnectionString" "<uri>" > /dev/null
```

### Seeding

```bash
# from backend/
dotnet user-secrets set "Seed__AdminPassword" "<password>" --project MSTailors.Api > /dev/null
dotnet run --project MSTailors.Api -- --seed            # idempotent; skips collections that have data
dotnet run --project MSTailors.Api -- --seed --force    # clears seeded collections first
```

Seeding never runs on normal startup. It exits without starting the web host,
and exits non-zero if `Seed__AdminPassword` is missing.

### Running the tests

Repository tests run against a real `mongod` started per test run, from
`tools/mongodb/bin/mongod` (gitignored, not committed). Set
`MSTAILORS_MONGOD_PATH` to point elsewhere. Without it those tests fail with a
message saying exactly what is missing.

```bash
curl -sL -o /tmp/mongodb.tgz https://fastdl.mongodb.org/osx/mongodb-macos-arm64-8.3.8.tgz
mkdir -p tools/mongodb && tar -xzf /tmp/mongodb.tgz -C tools/mongodb --strip-components=1
```

### Before saying work is done
Run `npm run build`, `npm run lint`, `npm run typecheck`, and `dotnet test` —
and paste the real output. Do not claim a build passes without having run it.

---

## 9. Working style

- **Phase discipline.** Do the phase asked, then stop and show the work.
- Do not scaffold, install, or generate beyond the current phase.
- Ask before: adding a dependency, changing the stack, adding a collection to
  the data model, or introducing a hardcoded brand.
- Report honestly. If something fails or was skipped, say so plainly.
