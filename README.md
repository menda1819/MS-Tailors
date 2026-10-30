# MS Tailors

> **Perfect Fit. Timeless Style.**
> *Tailored to You. Crafted with Care.*

The website and online shop for **MS Tailors** — a tailoring business at
No. 28 Station Road, Panadura, Sri Lanka.

The platform has two first-class sides:

1. **Tailoring services** — custom suits, wedding and business suits, shirts,
   trousers, blazers, uniforms, kids' formal wear, suit & coat rental, and
   alterations. This side generates **inquiries**: WhatsApp messages, calls, and
   measurement-appointment requests. There is no checkout here.
2. **A multi-brand online shop** — MS Tailors is the parent platform through
   which independent clothing brands sell ready-made products. Brands are
   created at runtime by an admin; **no brand is ever hardcoded.**

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js (App Router), TypeScript, Tailwind CSS |
| Backend | .NET Web API (latest LTS), C# |
| Database | MongoDB Atlas (`MongoDB.Driver`) |
| Auth | JWT bearer + rotating refresh tokens |
| Media | Cloudinary |
| Payments | PayHere (LKR) + Cash on Delivery |

---

## Repository layout

```
MS-Tailors/
├─ frontend/                      # Next.js app
├─ backend/
│  ├─ MSTailors.Api/              # controllers, DI, middleware, Program.cs
│  ├─ MSTailors.Application/      # services, DTOs, validation, interfaces
│  ├─ MSTailors.Domain/           # entities, enums, value objects
│  ├─ MSTailors.Infrastructure/   # Mongo context, repositories, external services
│  └─ MSTailors.Tests/
├─ docs/
│  └─ architecture.md             # architecture + data model
├─ CLAUDE.md                      # working agreement — read this first
└─ README.md
```

> **Status: Phase 0.** Only the documentation and repo scaffolding exist. The
> Next.js app and the .NET solution have not been created yet, so the commands
> below describe the intended workflow rather than something you can run today.

---

## Getting started

### Prerequisites
- Node.js 20+ and npm
- .NET SDK (latest LTS)
- A MongoDB Atlas cluster
- A Cloudinary account
- A PayHere merchant account (sandbox is fine for development)

### 1. Clone

```bash
git clone https://github.com/menda1819/MS-Tailors.git
cd MS-Tailors
```

### 2. Backend configuration

Secrets are **never** committed. In development they live in .NET user secrets:

```bash
cd backend/MSTailors.Api
dotnet user-secrets init
dotnet user-secrets set "MongoDb:ConnectionString" "<your-atlas-uri>"
dotnet user-secrets set "MongoDb:DatabaseName"     "mstailors"
dotnet user-secrets set "Jwt:Key"                  "<a-long-random-secret>"
dotnet user-secrets set "Jwt:Issuer"               "https://localhost:5001"
dotnet user-secrets set "Jwt:Audience"             "mstailors-web"
dotnet user-secrets set "Cloudinary:CloudName"     "<cloud-name>"
dotnet user-secrets set "Cloudinary:ApiKey"        "<api-key>"
dotnet user-secrets set "Cloudinary:ApiSecret"     "<api-secret>"
dotnet user-secrets set "PayHere:MerchantId"       "<merchant-id>"
dotnet user-secrets set "PayHere:MerchantSecret"   "<merchant-secret>"
```

`appsettings.Example.json` lists every key with placeholder values.

> **Colons here, double underscores in production.** User secrets and
> `appsettings.json` are JSON, so the separator is `:` — `MongoDb:ConnectionString`.
> Environment variables use `__` — `MongoDb__ConnectionString`. The `__` to `:`
> translation is an environment-variable convention only, so
> `dotnet user-secrets set "MongoDb__ConnectionString"` stores a literal key that
> **silently never binds**, and the app starts as if nothing were configured.

### 3. Frontend configuration

```bash
cd frontend
cp .env.example .env.local
```

```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
NEXT_PUBLIC_WHATSAPP_NUMBER=94XXXXXXXXX
NEXT_PUBLIC_SITE_URL=http://localhost:3000
```

Only `NEXT_PUBLIC_*` values reach the browser. Never put a secret behind that prefix.

### 4. Run

```bash
# terminal 1
cd backend && dotnet run --project MSTailors.Api

# terminal 2
cd frontend && npm install && npm run dev
```

---

## Commands

```bash
# Frontend
npm run dev          npm run build        npm run lint
npm run typecheck    npm test

# Backend
dotnet build         dotnet test          dotnet format
dotnet run --project MSTailors.Api
```

---

## Secrets policy

- Real credentials go in **.NET user secrets** (dev) and **environment
  variables** (prod) — never in the repository.
- `.env`, `.env.local`, `appsettings.Development.json`, and `secrets.json` are
  gitignored.
- `.env.example` and `appsettings.Example.json` contain **placeholders only**.
- If you find a real credential in a tracked file, stop and report it before
  committing.

---

## Documentation

- [`CLAUDE.md`](./CLAUDE.md) — business context, conventions, design direction,
  and the working agreement for this repo.
- [`docs/architecture.md`](./docs/architecture.md) — architecture and data model.

---

## Contact

**MS Tailors** · No. 28 Station Road, Panadura, Sri Lanka
