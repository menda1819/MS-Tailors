# Architecture & Data Model — MS Tailors

**Status:** proposed, Phase 0. Nothing here is built yet. This document is for
review before the Next.js app and .NET solution are scaffolded.

---

## 1. System shape

```
                        ┌──────────────────────────────┐
   Browser  ───────────▶│  Next.js (App Router)        │
                        │  SSR / SSG / ISR · RSC       │
                        └──────────────┬───────────────┘
                                       │  HTTPS, typed API client
                                       ▼
                        ┌──────────────────────────────┐
                        │  .NET Web API                │
                        │  Api → Application → Domain  │
                        │        └→ Infrastructure     │
                        └───┬────────────┬─────────────┘
                            │            │
                  ┌─────────▼──┐   ┌─────▼────────┐   ┌──────────────┐
                  │ MongoDB    │   │ Cloudinary   │   │ PayHere      │
                  │ Atlas      │   │ (media)      │   │ (LKR gateway)│
                  └────────────┘   └──────────────┘   └──────────────┘
```

Two independently deployable apps in one repo. The frontend never talks to
Mongo, Cloudinary, or PayHere directly — everything goes through the API.

### Why this split
- **SEO is a hard requirement.** Public pages render on the server, so product
  and brand pages are indexable without a JS execution step.
- **Prices must be trustworthy.** Totals are computed in the API from database
  values, never from the cart the browser posts.
- **Brands are runtime data.** Nothing about a brand is baked into a build; a
  new brand appears through the admin API and the shop picks it up.

### Backend layering

| Project | Knows about | Must never reference |
|---|---|---|
| `MSTailors.Domain` | nothing but the BCL | Mongo, ASP.NET, DTOs |
| `MSTailors.Application` | Domain, interfaces, DTOs, validators | `MongoDB.Driver`, HTTP types |
| `MSTailors.Infrastructure` | Mongo, Cloudinary, PayHere, SMTP | controllers |
| `MSTailors.Api` | Application + DI wiring | Mongo directly |

The Application layer owns the interfaces (`IProductRepository`, `IBrandRepository`,
`IPaymentProvider`, `IMediaStore`, `IEmailSender`); Infrastructure implements
them. Swapping Cloudinary or adding a second payment gateway touches one project.

### Request flow, worked example — `POST /api/orders`

```
Controller           binds CreateOrderRequest, no logic
  → FluentValidation shape/format only → 400 ProblemDetails on failure
  → OrderService     loads every product+variant from Mongo by id
                     recomputes unit prices, line totals, delivery, grand total
                     checks stock, checks the brand and product are active
                     rejects on ANY mismatch with what the browser sent
  → IOrderRepository inserts the order with status PendingPayment
  → IPaymentProvider builds the PayHere redirect payload (COD skips this)
  → 201 Created      returns orderNumber + payment redirect data
```

The client-sent price is used for **one thing only**: comparing against the
server total so a mismatch can be logged and rejected.

---

## 2. Rendering strategy

| Route | Strategy | Note |
|---|---|---|
| `/` | ISR | Both paths — tailoring and shop — visible above the fold |
| `/services`, `/services/[slug]` | SSG | Rebuilt when services change |
| `/gallery`, `/about`, `/contact` | SSG | |
| `/appointment` | SSG shell + client form | Posts to the API |
| `/shop` | ISR | Filter/sort state in the URL, so it stays crawlable |
| `/brands` | ISR | Empty state must be designed, not an error |
| `/brands/[slug]` | ISR + `generateStaticParams` | Brand identity, story, collection |
| `/shop/[brandSlug]/[productSlug]` | ISR + `generateStaticParams` | `Product` JSON-LD |
| `/cart`, `/checkout` | Client | Never indexed |
| `/orders/[orderNumber]` | SSR | Guest lookup by order number + email |
| `/admin/**` | Client, `noindex` | JWT-gated |

`sitemap.ts` and `robots.ts` are generated from the API so new brands and
products enter the sitemap automatically.

---

## 3. Data model (MongoDB — database `mstailors`)

Conventions for every collection: `_id` is an ObjectId exposed as `string`;
field names are `camelCase`; timestamps are UTC; **money is integer LKR cents**
(`priceCents`), never a floating-point number.

### 3.1 `brands`
Created at runtime by an admin. Nothing about a brand is hardcoded.

```jsonc
{
  "_id": ObjectId,
  "name": "string",                  // unique, admin-entered
  "slug": "string",                  // unique, indexed — /brands/{slug}
  "tagline": "string?",
  "story": "string",                 // rich text / markdown
  "logo":  { "publicId": "string", "url": "string", "alt": "string" },
  "hero":  { "publicId": "string", "url": "string", "alt": "string",
             "width": 0, "height": 0, "blurDataUrl": "string?" },
  "accentColor": "#RRGGBB?",         // optional per-brand accent, validated
  "seo": { "title": "string?", "description": "string?", "ogImageUrl": "string?" },
  "contact": { "email": "string?", "phone": "string?", "website": "string?" },
  "isActive": true,
  "isFeatured": false,
  "sortOrder": 0,
  "productCount": 0,                 // denormalised, recomputed on product write
  "isDeleted": false,
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{slug:1}` unique · `{isActive:1, isDeleted:1, sortOrder:1}` ·
`{isFeatured:1, isActive:1}`

### 3.2 `categories`
Shared shop taxonomy, one level of nesting. Independent of brands, so the same
category spans brands.

```jsonc
{
  "_id": ObjectId,
  "name": "string", "slug": "string",         // unique
  "parentId": "ObjectId?",                    // null = top level
  "description": "string?",
  "image": { "publicId": "string", "url": "string", "alt": "string" },
  "sortOrder": 0, "isActive": true,
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{slug:1}` unique · `{parentId:1, sortOrder:1}`

### 3.3 `products`
Ready-made shop items. Every product belongs to exactly one brand. Stock and
price live on the **variant**, which is the unit that is actually sold.

```jsonc
{
  "_id": ObjectId,
  "brandId": ObjectId,               // required — the ownership key
  "brandSlug": "string",             // denormalised for URL + list rendering
  "brandName": "string",             // denormalised for list rendering
  "categoryId": ObjectId,
  "name": "string",
  "slug": "string",                  // unique within a brand
  "description": "string",
  "shortDescription": "string?",
  "material": "string?",
  "careInstructions": "string?",
  "images": [                        // first image is the card image
    { "publicId": "string", "url": "string", "alt": "string",
      "width": 0, "height": 0, "blurDataUrl": "string?", "sortOrder": 0 }
  ],
  "variants": [
    { "sku": "string",               // unique across all products
      "size": "string",              // "M", "40R", "32x34" — free text, brand-defined
      "color": "string?",
      "colorHex": "#RRGGBB?",
      "priceCents": 0,               // LKR cents — the source of truth for price
      "compareAtPriceCents": 0,      // optional strike-through price
      "stock": 0,
      "isActive": true }
  ],
  "priceRange": { "minCents": 0, "maxCents": 0 },   // derived, for list/sort
  "totalStock": 0,                                   // derived, sum of variants
  "tags": ["string"],
  "seo": { "title": "string?", "description": "string?" },
  "isActive": true, "isFeatured": false, "isDeleted": false,
  "publishedAt": ISODate,
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{brandSlug:1, slug:1}` unique · `{brandId:1, isActive:1, isDeleted:1}` ·
`{categoryId:1, isActive:1}` · `{isFeatured:1, isActive:1}` ·
`{"priceRange.minCents":1}` · `{"variants.sku":1}` unique sparse ·
text index on `{name, description, tags}` for search.

> `brandSlug` / `brandName` are denormalised so a shop grid is one query instead
> of a lookup per card. Renaming a brand triggers a background backfill — that
> write path must exist before the admin can rename a brand.

### 3.4 `services`
The tailoring side. Content-managed so copy changes without a deploy. **No
prices are published** — these pages drive inquiries.

```jsonc
{
  "_id": ObjectId,
  "name": "string",                  // "Wedding Suits", "Alterations"
  "slug": "string",                  // unique
  "category": "Suits | Shirts | Trousers | Uniforms | Kidswear | Rental | Alterations",
  "summary": "string",
  "description": "string",
  "features": ["string"],            // "Full canvas construction", "3 fittings"
  "priceGuide": "string?",           // free text only, e.g. "From LKR 45,000"
  "turnaroundDays": "string?",       // "10–14 days"
  "isRental": false,
  "images": [ { "publicId": "string", "url": "string", "alt": "string" } ],
  "seo": { "title": "string?", "description": "string?" },
  "isActive": true, "sortOrder": 0,
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{slug:1}` unique · `{isActive:1, sortOrder:1}` · `{category:1}`

### 3.5 `inquiries`
Every non-shop conversion lands here: contact form, measurement appointment,
rental request, alteration request.

```jsonc
{
  "_id": ObjectId,
  "referenceNumber": "INQ-2026-0001",
  "type": "General | Appointment | Rental | Alteration | Quote",
  "name": "string", "phone": "string", "email": "string?",
  "serviceId": "ObjectId?", "serviceName": "string?",
  "message": "string",
  "preferredDate": "ISODate?",       // appointment requests
  "preferredTime": "Morning | Afternoon | Evening | null",
  "eventDate": "ISODate?",           // wedding/rental date
  "status": "New | Contacted | Scheduled | Completed | Closed",
  "adminNotes": "string?",
  "source": "web | whatsapp",
  "ipHash": "string",                // rate limiting + abuse, hashed not raw
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{referenceNumber:1}` unique · `{status:1, createdAt:-1}` ·
`{createdAt:-1}` · `{phone:1}`

### 3.6 `orders`
Shop orders only. **A snapshot, not a set of references** — an order must still
render correctly after a product is renamed, repriced, or deleted.

```jsonc
{
  "_id": ObjectId,
  "orderNumber": "MST-2026-000123",  // unique, from the counters collection
  "customerId": "ObjectId?",         // null for guest checkout
  "customer": { "name": "string", "email": "string", "phone": "string" },
  "shippingAddress": {
    "line1": "string", "line2": "string?", "city": "string",
    "district": "string", "postalCode": "string?", "country": "LK"
  },
  "items": [
    { "productId": ObjectId, "variantSku": "string",
      "brandId": ObjectId, "brandName": "string",     // snapshot
      "productName": "string", "size": "string", "color": "string?",
      "imageUrl": "string",
      "unitPriceCents": 0,           // server-resolved at order time
      "quantity": 0,
      "lineTotalCents": 0 }
  ],
  "subtotalCents": 0,
  "deliveryFeeCents": 0,
  "discountCents": 0,
  "totalCents": 0,                   // all four recomputed server-side
  "currency": "LKR",
  "payment": {
    "method": "PayHere | CashOnDelivery",
    "status": "Pending | Paid | Failed | Refunded",
    "providerReference": "string?",  // PayHere payment id
    "paidAt": "ISODate?"
  },
  "status": "PendingPayment | Confirmed | Processing | Shipped | Delivered | Cancelled",
  "statusHistory": [ { "status": "string", "at": ISODate, "note": "string?" } ],
  "notes": "string?",
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{orderNumber:1}` unique · `{"customer.email":1, createdAt:-1}` ·
`{status:1, createdAt:-1}` · `{"payment.status":1}` · `{customerId:1, createdAt:-1}`

### 3.7 `users`
One collection for admins and optional customer accounts, separated by role.

```jsonc
{
  "_id": ObjectId,
  "email": "string",                 // unique, lowercased
  "passwordHash": "string",          // ASP.NET Core Identity hasher / BCrypt
  "name": "string",
  "phone": "string?",
  "role": "Admin | Staff | Customer",   // BrandOwner reserved for a later phase
  "brandId": "ObjectId?",               // reserved: scopes a future BrandOwner
  "addresses": [ { "label": "string", "line1": "string", "line2": "string?",
                   "city": "string", "district": "string",
                   "postalCode": "string?", "isDefault": false } ],
  "isActive": true,
  "lastLoginAt": "ISODate?",
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{email:1}` unique · `{role:1}`

### 3.8 `refreshTokens`
Rotating refresh tokens, one document per issued token.

```jsonc
{
  "_id": ObjectId,
  "userId": ObjectId,
  "tokenHash": "string",             // SHA-256 of the token — never the raw value
  "expiresAt": ISODate,
  "revokedAt": "ISODate?",
  "replacedByTokenHash": "string?",  // rotation chain, detects token reuse
  "createdByIpHash": "string?",
  "createdAt": ISODate
}
```

Indexes: `{tokenHash:1}` unique · `{userId:1}` ·
`{expiresAt:1}` **TTL** so expired tokens are reaped automatically.

### 3.9 `gallery`
Portfolio of finished work — the strongest sales asset for the tailoring side.

```jsonc
{
  "_id": ObjectId,
  "title": "string", "caption": "string?",
  "category": "Wedding | Business | Casual | Uniform | Kidswear | Alteration",
  "image": { "publicId": "string", "url": "string", "alt": "string",
             "width": 0, "height": 0, "blurDataUrl": "string?" },
  "serviceId": "ObjectId?",
  "sortOrder": 0, "isActive": true, "isFeatured": false,
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{category:1, isActive:1, sortOrder:1}` · `{isFeatured:1}`

### 3.10 `testimonials`

```jsonc
{
  "_id": ObjectId,
  "customerName": "string", "role": "string?",
  "quote": "string", "rating": 5,
  "image": { "publicId": "string", "url": "string", "alt": "string" },
  "isApproved": false, "isFeatured": false, "sortOrder": 0,
  "createdAt": ISODate, "updatedAt": ISODate
}
```

Indexes: `{isApproved:1, isFeatured:1, sortOrder:1}`

### 3.11 `siteSettings` — single document
Business details the client can edit without a deploy. Consumed by the footer,
contact page, and `LocalBusiness` JSON-LD.

```jsonc
{
  "_id": ObjectId,
  "businessName": "MS Tailors",
  "tagline": "Perfect Fit. Timeless Style.",
  "secondaryTagline": "Tailored to You. Crafted with Care.",
  "address": { "line1": "No. 28 Station Road", "city": "Panadura",
               "district": "Kalutara", "country": "LK",
               "geo": { "lat": 0.0, "lng": 0.0 } },
  "phone": "string", "whatsappNumber": "string", "email": "string",
  "openingHours": [ { "day": "Monday", "opens": "09:00", "closes": "18:00",
                      "isClosed": false } ],
  "socialLinks": { "facebook": "string?", "instagram": "string?", "tiktok": "string?" },
  "deliveryFeeCents": 0,
  "freeDeliveryThresholdCents": 0,
  "announcementBar": { "text": "string?", "isActive": false },
  "updatedAt": ISODate
}
```

### 3.12 `counters`
Atomic sequence generator for human-readable numbers (`findOneAndUpdate` with
`$inc`), used by `orders.orderNumber` and `inquiries.referenceNumber`.

```jsonc
{ "_id": "orders-2026", "sequence": 123 }
```

---

## 4. Relationships

```
brands 1───∞ products                    (products.brandId, cascade → soft-delete)
categories 1───∞ products                (products.categoryId)
products ∞───1 orders (snapshotted)      (order items copy, never reference)
users 1───∞ refreshTokens                (TTL-reaped)
users 1───∞ orders                       (nullable — guest checkout)
services 1───∞ inquiries                 (optional)
services 1───∞ gallery                   (optional)
```

**Snapshot vs reference.** Orders snapshot; everything else references.
An order from last year must render with the price and name that were true when
it was placed, even if the product has since been deleted.

**Brand deletion is a soft delete.** Deactivating a brand hides its products
from the shop but leaves historical orders intact.

---

## 5. API surface (proposed)

Base: `/api`. Public endpoints are anonymous; `[Admin]` requires an
`Admin`/`Staff` JWT.

### Public — shop
```
GET  /brands                        ?featured&page&pageSize
GET  /brands/{slug}
GET  /brands/{slug}/products        ?category&minPrice&maxPrice&sort&page
GET  /products                      ?brand&category&q&minPrice&maxPrice&sort&page
GET  /products/{brandSlug}/{slug}
GET  /categories
POST /orders                        # totals recomputed server-side
GET  /orders/{orderNumber}          ?email=  # guest lookup
POST /payments/payhere/notify       # server-to-server IPN, signature-verified
```

### Public — tailoring
```
GET  /services            GET /services/{slug}
GET  /gallery             ?category
GET  /testimonials
GET  /site-settings
POST /inquiries           # rate-limited, honeypot + server validation
```

### Auth
```
POST /auth/register       POST /auth/login
POST /auth/refresh        POST /auth/logout
GET  /auth/me
```

### Admin
```
[Admin] POST|PUT|DELETE /admin/brands/{id?}
[Admin] POST|PUT|DELETE /admin/products/{id?}
[Admin] POST|PUT|DELETE /admin/services/{id?}
[Admin] POST|PUT|DELETE /admin/gallery/{id?}
[Admin] GET|PATCH        /admin/inquiries/{id?}
[Admin] GET|PATCH        /admin/orders/{id?}
[Admin] PUT              /admin/site-settings
[Admin] POST             /admin/media/signature   # signed direct Cloudinary upload
```

Responses are envelope-free JSON; errors are RFC 7807 `ProblemDetails`.
Lists return `{ items, page, pageSize, total }`.

---

## 6. Cross-cutting concerns

**Auth.** Short-lived access JWT (15 min) in memory on the client; refresh token
in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie. Refresh rotates and stores
only a hash; presenting a used token revokes the whole chain.

**Never trust the client.** Prices, totals, stock, and discounts are always read
from Mongo at order time. Stock decrements happen in the same operation that
confirms payment, guarded so a variant cannot go negative.

**Payments.** PayHere redirect + server-to-server IPN. An order is `Paid` only
when the **signature-verified IPN** says so — never because the browser returned
to the success URL. COD skips the gateway and lands in `Confirmed`.

**Media.** The admin uploads straight to Cloudinary using a signature the API
issues; the API secret never reaches the browser. Only `publicId` + derived URLs
are stored.

**Validation.** FluentValidation for shape and format at the edge; business
rules (stock, active brand, price agreement) in the service layer, where they
can see the database.

**Rate limiting.** ASP.NET Core rate limiter on `/inquiries`, `/auth/*`, and
`/orders`, plus a honeypot field on public forms.

**CORS.** Origins from `Cors__AllowedOrigins`. No wildcard in production.

**Logging.** Structured logs with a correlation id. **Never log a JWT, a refresh
token, a payment secret, or a connection string.**

---

## 7. Open questions for Phase 1+

1. **Brand rename backfill** — background job, or block renames after the first
   product exists?
2. **Sinhala localisation** — is `next-intl` in scope, or English-only for v1?
3. **Delivery pricing** — flat island-wide fee, or per-district rates?
4. **Order notification email** — SMTP, or WhatsApp/SMS only?
5. **Appointment slots** — free-text preferred time (as modelled), or a real
   calendar with capacity per slot?
