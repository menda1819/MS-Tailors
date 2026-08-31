/** Display formatting. Every price on the admin goes through here. */

const currencyFormatter = new Intl.NumberFormat("en-LK", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/** LKR 34,500.00 — the currency code reads more clearly than the ₨ glyph here. */
export function formatMoney(amount: number, currency = "LKR"): string {
  return `${currency} ${currencyFormatter.format(amount)}`;
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat("en-LK").format(value);
}

export function formatDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("en-LK", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("en-LK", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

/** "3 days ago" for recency, falling back to a date once that stops being useful. */
export function formatRelative(iso: string | null | undefined): string {
  if (!iso) return "—";

  const then = new Date(iso).getTime();
  const seconds = Math.round((Date.now() - then) / 1000);

  if (seconds < 60) return "just now";
  if (seconds < 3600) return `${Math.floor(seconds / 60)} min ago`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)} hr ago`;
  if (seconds < 604800) return `${Math.floor(seconds / 86400)} d ago`;

  return formatDate(iso);
}

/** Turns a product name into a URL slug as the admin types it. */
export function slugify(value: string): string {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 120);
}

/**
 * "1 order", "3 orders". Irregular plurals pass an explicit form.
 *
 * Worth a helper rather than inline ternaries: an admin that says "1 orders"
 * reads as unfinished, and it is the kind of thing that is wrong in a dozen
 * places at once.
 */
export function pluralise(count: number, singular: string, plural?: string): string {
  return count === 1 ? singular : (plural ?? `${singular}s`);
}

/** "1 order", "12 orders" — the count and its noun together. */
export function countOf(count: number, singular: string, plural?: string): string {
  return `${formatNumber(count)} ${pluralise(count, singular, plural)}`;
}
