"use client";

import Link from "next/link";
import { useResource } from "@/components/admin/use-paged";
import {
  Badge,
  Button,
  Card,
  CardHeader,
  EmptyState,
  ErrorState,
  PageHeader,
  Skeleton,
} from "@/components/ui/primitives";
import { countOf, formatDateTime, formatMoney, formatNumber } from "@/lib/format";
import type { DashboardData, OrderStatus } from "@/types/admin";

/** Only statuses that need someone to do something are surfaced as work. */
const ACTIONABLE: OrderStatus[] = ["New", "Confirmed", "Processing", "Packed"];

const STATUS_TONE: Record<OrderStatus, "neutral" | "accent" | "success" | "warning" | "danger"> = {
  New: "accent",
  Confirmed: "accent",
  Processing: "warning",
  Packed: "warning",
  Shipped: "accent",
  Delivered: "success",
  Cancelled: "danger",
  Returned: "danger",
};

function Metric({
  label,
  value,
  detail,
}: {
  label: string;
  value: string;
  detail?: string;
}) {
  return (
    <Card className="p-5">
      <p className="text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
        {label}
      </p>
      <p className="mt-2 font-display text-2xl leading-none text-ink tabular">{value}</p>
      {detail && <p className="mt-2 text-[0.8125rem] text-slate">{detail}</p>}
    </Card>
  );
}

function DashboardSkeleton() {
  return (
    <div className="flex flex-col gap-6" aria-busy="true" aria-label="Loading dashboard">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <Skeleton key={index} className="h-28 w-full" />
        ))}
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Skeleton className="h-72 w-full" />
        <Skeleton className="h-72 w-full" />
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const { data, loading, error, reload } = useResource<DashboardData>("/api/admin/dashboard");

  if (loading) {
    return (
      <>
        <PageHeader eyebrow="Overview" title="Dashboard" />
        <DashboardSkeleton />
      </>
    );
  }

  if (error || !data) {
    return (
      <>
        <PageHeader eyebrow="Overview" title="Dashboard" />
        <ErrorState
          message={error ?? "The dashboard could not be loaded."}
          onRetry={reload}
        />
      </>
    );
  }

  const outstanding = data.ordersByStatus
    .filter((entry) => ACTIONABLE.includes(entry.status) && entry.count > 0)
    .reduce((sum, entry) => sum + entry.count, 0);

  const outOfStock = data.lowStockVariants.filter((v) => v.stockQuantity === 0);

  return (
    <>
      <PageHeader
        eyebrow="Overview"
        title="Dashboard"
        description={`Figures as at ${formatDateTime(data.generatedAtUtc)}.`}
        actions={
          <Button variant="secondary" size="sm" onClick={reload}>
            Refresh
          </Button>
        }
      />

      <div className="flex flex-col gap-6">
        <section aria-label="Sales" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <Metric
            label="Sales today"
            value={formatMoney(data.sales.today, data.sales.currency)}
            detail={`${countOf(data.todayOrders, "order")} placed`}
          />
          <Metric
            label="Sales this month"
            value={formatMoney(data.sales.month, data.sales.currency)}
          />
          <Metric
            label="Sales all time"
            value={formatMoney(data.sales.total, data.sales.currency)}
            detail={`${countOf(data.totalOrders, "order")} in total`}
          />
          <Metric
            label="Registered customers"
            value={formatNumber(data.customerCount)}
            detail={
              data.newInquiryCount > 0
                ? `${countOf(data.newInquiryCount, "inquiry", "inquiries")} awaiting reply`
                : "No inquiries waiting"
            }
          />
        </section>

        {/* What needs doing, before what has already happened. */}
        {(outstanding > 0 || outOfStock.length > 0 || data.newInquiryCount > 0) && (
          <Card className="border-indigo-soft bg-indigo-soft/40 p-5">
            <h2 className="font-display text-lg text-ink">Needs attention</h2>
            <ul className="mt-3 flex flex-col gap-2 text-sm text-ink">
              {outstanding > 0 && (
                <li>
                  <Link href="/admin/orders" className="text-indigo underline underline-offset-2">
                    {countOf(outstanding, "order")} still to fulfil
                  </Link>
                </li>
              )}
              {data.newInquiryCount > 0 && (
                <li>
                  <Link href="/admin/inquiries" className="text-indigo underline underline-offset-2">
                    {countOf(data.newInquiryCount, "inquiry", "inquiries")} not yet answered
                  </Link>
                </li>
              )}
              {outOfStock.length > 0 && (
                <li>
                  <Link href="/admin/inventory" className="text-indigo underline underline-offset-2">
                    {countOf(outOfStock.length, "variant")} out of stock
                  </Link>
                </li>
              )}
            </ul>
          </Card>
        )}

        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <CardHeader
              title="Orders by status"
              actions={
                <Link href="/admin/orders" className="text-[0.8125rem] text-indigo underline underline-offset-2">
                  View all
                </Link>
              }
            />
            <div className="flex flex-col">
              {data.ordersByStatus.filter((entry) => entry.count > 0).length === 0 ? (
                <EmptyState
                  title="No orders yet"
                  description="Once customers start buying, their orders will appear here grouped by where they are in fulfilment."
                />
              ) : (
                data.ordersByStatus
                  .filter((entry) => entry.count > 0)
                  .map((entry) => (
                    <Link
                      key={entry.status}
                      href={`/admin/orders?status=${entry.status}`}
                      className="flex items-center justify-between border-b border-sand-soft/70 px-5 py-3 last:border-0 hover:bg-paper-sunk/60"
                    >
                      <Badge tone={STATUS_TONE[entry.status]}>{entry.status}</Badge>
                      <span className="text-sm text-ink tabular">{formatNumber(entry.count)}</span>
                    </Link>
                  ))
              )}
            </div>
          </Card>

          <Card>
            <CardHeader title="Best sellers" description="By units sold, all time." />
            <div className="flex flex-col">
              {data.bestSellers.length === 0 ? (
                <EmptyState
                  title="Nothing sold yet"
                  description="Your best-selling products will be listed here once orders start coming in."
                />
              ) : (
                data.bestSellers.map((product) => (
                  <div
                    key={product.productId}
                    className="flex items-center justify-between gap-4 border-b border-sand-soft/70 px-5 py-3 last:border-0"
                  >
                    <span className="min-w-0 truncate text-sm text-ink">{product.productName}</span>
                    <span className="shrink-0 text-sm text-slate tabular">
                      {formatNumber(product.unitsSold)} sold
                    </span>
                  </div>
                ))
              )}
            </div>
          </Card>

          <Card>
            <CardHeader
              title="Brand performance"
              description="Revenue attributed from order lines, so it survives a product being repriced."
            />
            <div className="flex flex-col">
              {data.bestPerformingBrands.length === 0 ? (
                <EmptyState
                  title="No brands yet"
                  description="Add a brand and its products to start tracking how each one performs."
                />
              ) : (
                data.bestPerformingBrands.map((brand) => (
                  <div
                    key={brand.brandId}
                    className="flex items-center justify-between gap-4 border-b border-sand-soft/70 px-5 py-3 last:border-0"
                  >
                    <div className="min-w-0">
                      <p className="truncate text-sm text-ink">{brand.brandName}</p>
                      <p className="text-xs text-slate tabular">
                        {countOf(brand.productCount, "product")} ·{" "}
                        {countOf(brand.unitsSold, "unit")} sold
                      </p>
                    </div>
                    <span className="shrink-0 text-sm text-ink tabular">
                      {formatMoney(brand.revenue)}
                    </span>
                  </div>
                ))
              )}
            </div>
          </Card>

          <Card>
            <CardHeader
              title="Low stock"
              description="Variants at or below five units."
              actions={
                <Link href="/admin/inventory" className="text-[0.8125rem] text-indigo underline underline-offset-2">
                  Manage
                </Link>
              }
            />
            <div className="flex flex-col">
              {data.lowStockVariants.length === 0 ? (
                <EmptyState
                  title="Stock is healthy"
                  description="No variant is down to its last five units."
                />
              ) : (
                data.lowStockVariants.slice(0, 8).map((variant) => (
                  <div
                    key={`${variant.productId}-${variant.sku}`}
                    className="flex items-center justify-between gap-4 border-b border-sand-soft/70 px-5 py-3 last:border-0"
                  >
                    <div className="min-w-0">
                      <p className="truncate text-sm text-ink">{variant.productName}</p>
                      <p className="text-xs text-slate">
                        {variant.size} · {variant.color}
                      </p>
                    </div>
                    <Badge tone={variant.stockQuantity === 0 ? "danger" : "warning"}>
                      {variant.stockQuantity === 0
                        ? "Out of stock"
                        : `${variant.stockQuantity} left`}
                    </Badge>
                  </div>
                ))
              )}
            </div>
          </Card>
        </div>
      </div>
    </>
  );
}
