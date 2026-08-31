"use client";

import { ResourceManager, type FieldDef } from "@/components/admin/resource-manager";
import { Badge } from "@/components/ui/primitives";
import { formatDate, formatMoney } from "@/lib/format";
import type { Coupon } from "@/types/admin";

const FIELDS: FieldDef[] = [
  {
    name: "code",
    label: "Code",
    kind: "text",
    required: true,
    span: "half",
    hint: "Letters, numbers and hyphens. Customers type this at checkout.",
  },
  {
    name: "type",
    label: "Discount type",
    kind: "select",
    required: true,
    span: "half",
    options: [
      { value: "Percentage", label: "Percentage off" },
      { value: "FixedAmount", label: "Fixed amount off" },
    ],
  },
  {
    name: "value",
    label: "Discount value",
    kind: "money",
    required: true,
    span: "half",
    hint: "A percentage from 1 to 100, or an amount in LKR.",
  },
  {
    name: "maxDiscount",
    label: "Maximum discount",
    kind: "money",
    span: "half",
    hint: "Caps a percentage discount. Leave blank for no cap.",
  },
  {
    name: "minOrderTotal",
    label: "Minimum order total",
    kind: "money",
    span: "half",
    hint: "The coupon does nothing below this subtotal.",
  },
  {
    name: "usageLimit",
    label: "Usage limit",
    kind: "number",
    span: "half",
    hint: "Total redemptions allowed. Leave blank for unlimited.",
  },
  { name: "startsAt", label: "Valid from", kind: "datetime", required: true, span: "half" },
  { name: "expiresAt", label: "Valid until", kind: "datetime", required: true, span: "half" },
  { name: "isActive", label: "Active", kind: "checkbox" },
];

function statusOf(coupon: Coupon): { label: string; tone: "success" | "warning" | "danger" | "neutral" } {
  const now = Date.now();

  if (!coupon.isActive) return { label: "Inactive", tone: "neutral" };
  if (new Date(coupon.expiresAt).getTime() < now) return { label: "Expired", tone: "danger" };
  if (new Date(coupon.startsAt).getTime() > now) return { label: "Scheduled", tone: "warning" };
  if (coupon.usageLimit !== null && coupon.usedCount >= coupon.usageLimit) {
    return { label: "Used up", tone: "danger" };
  }

  return { label: "Live", tone: "success" };
}

export default function CouponsPage() {
  return (
    <ResourceManager<Coupon>
      eyebrow="Catalogue"
      title="Coupons"
      description="Discount codes for the shop. Usage counts are maintained by checkout and cannot be edited here."
      endpoint="/api/admin/coupons"
      rowKey={(coupon) => coupon.id}
      itemNoun="coupon"
      itemNounPlural="coupons"
      describeRow={(coupon) => coupon.code}
      emptyTitle="No coupons yet"
      emptyDescription="Create a discount code to run a promotion. Codes are case-insensitive at checkout."
      columns={[
        {
          key: "code",
          header: "Code",
          render: (coupon) => (
            <code className="rounded-tailored bg-paper-sunk px-2 py-1 text-xs font-medium tracking-wide text-ink">
              {coupon.code}
            </code>
          ),
        },
        {
          key: "discount",
          header: "Discount",
          render: (coupon) =>
            coupon.type === "Percentage"
              ? `${coupon.value}% off${coupon.maxDiscount ? `, up to ${formatMoney(coupon.maxDiscount)}` : ""}`
              : `${formatMoney(coupon.value)} off`,
        },
        {
          key: "minimum",
          header: "Minimum",
          secondary: true,
          align: "right",
          render: (coupon) => (coupon.minOrderTotal > 0 ? formatMoney(coupon.minOrderTotal) : "—"),
        },
        {
          key: "used",
          header: "Used",
          align: "right",
          render: (coupon) => (
            <span className="tabular text-sm">
              {coupon.usedCount}
              {coupon.usageLimit !== null ? ` / ${coupon.usageLimit}` : ""}
            </span>
          ),
        },
        {
          key: "window",
          header: "Valid",
          secondary: true,
          render: (coupon) => (
            <span className="text-xs text-slate">
              {formatDate(coupon.startsAt)} – {formatDate(coupon.expiresAt)}
            </span>
          ),
        },
        {
          key: "status",
          header: "Status",
          render: (coupon) => {
            const status = statusOf(coupon);
            return <Badge tone={status.tone}>{status.label}</Badge>;
          },
        },
      ]}
      fields={FIELDS}
      toForm={(coupon) => ({
        code: coupon.code,
        type: coupon.type,
        value: coupon.value,
        minOrderTotal: coupon.minOrderTotal,
        maxDiscount: coupon.maxDiscount,
        usageLimit: coupon.usageLimit,
        startsAt: coupon.startsAt,
        expiresAt: coupon.expiresAt,
        isActive: coupon.isActive,
      })}
      emptyForm={() => ({
        code: "",
        type: "Percentage",
        value: 10,
        minOrderTotal: 0,
        maxDiscount: null,
        usageLimit: null,
        // Read the clock when the form opens, not while rendering the page.
        startsAt: new Date().toISOString(),
        expiresAt: new Date(Date.now() + 30 * 86_400_000).toISOString(),
        isActive: true,
      })}
    />
  );
}
