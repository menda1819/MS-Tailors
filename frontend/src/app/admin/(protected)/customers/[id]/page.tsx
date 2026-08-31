"use client";

import { use } from "react";
import Link from "next/link";
import { useResource } from "@/components/admin/use-paged";
import { STATUS_TONE } from "@/app/admin/(protected)/orders/page";
import {
  Badge,
  Card,
  CardHeader,
  EmptyState,
  ErrorState,
  PageHeader,
  Skeleton,
} from "@/components/ui/primitives";
import { countOf, formatDate, formatDateTime, formatMoney } from "@/lib/format";
import type { AdminCustomerDetail } from "@/types/admin";

export default function CustomerDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data, loading, error, reload } = useResource<AdminCustomerDetail>(
    `/api/admin/customers/${id}`,
  );

  if (loading) {
    return (
      <>
        <PageHeader eyebrow="People" title="Customer" />
        <Skeleton className="h-72 w-full" />
      </>
    );
  }

  if (error || !data) {
    return (
      <>
        <PageHeader eyebrow="People" title="Customer" />
        <ErrorState message={error ?? "That customer could not be loaded."} onRetry={reload} />
      </>
    );
  }

  const { customer } = data;

  return (
    <>
      <PageHeader
        eyebrow={
          <Link href="/admin/customers" className="text-indigo underline underline-offset-2">
            Customers
          </Link>
        }
        title={customer.fullName}
        description={`Joined ${formatDate(customer.createdAt)}`}
      />

      <div className="mb-4 flex flex-wrap gap-2">
        {customer.role === "Admin" && <Badge tone="accent">Staff</Badge>}
        <Badge tone={customer.isActive ? "success" : "danger"}>
          {customer.isActive ? "Active" : "Disabled"}
        </Badge>
        <Badge tone={customer.emailVerified ? "success" : "warning"}>
          {customer.emailVerified ? "Email verified" : "Email unverified"}
        </Badge>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="flex flex-col gap-4">
          <Card>
            <CardHeader title="Contact" />
            <div className="flex flex-col gap-1 px-5 py-4 text-sm">
              <a href={`mailto:${customer.email}`} className="text-indigo underline underline-offset-2">
                {customer.email}
              </a>
              <a href={`tel:${customer.phone}`} className="text-indigo underline underline-offset-2">
                {customer.phone}
              </a>
              <p className="mt-2 text-xs text-slate">
                Last signed in {formatDateTime(customer.lastLoginAt)}
              </p>
            </div>
          </Card>

          <Card>
            <CardHeader title="Value" />
            <div className="flex flex-col gap-3 px-5 py-4">
              <div>
                <p className="text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                  Lifetime value
                </p>
                <p className="mt-1 font-display text-xl text-ink tabular">
                  {formatMoney(data.lifetimeValue, data.currency)}
                </p>
                <p className="mt-1 text-xs text-slate">
                  Excludes cancelled and returned orders.
                </p>
              </div>
              <div>
                <p className="text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                  Orders
                </p>
                <p className="mt-1 font-display text-xl text-ink tabular">{data.orderCount}</p>
              </div>
            </div>
          </Card>

          <Card>
            <CardHeader title="Addresses" />
            {data.addresses.length === 0 ? (
              <p className="px-5 py-6 text-sm text-slate">No saved addresses.</p>
            ) : (
              <ul className="flex flex-col">
                {data.addresses.map((address, index) => (
                  <li key={index} className="border-b border-sand-soft/70 px-5 py-4 last:border-0">
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-ink">{address.label}</p>
                      {address.isDefault && <Badge tone="accent">Default</Badge>}
                    </div>
                    <address className="mt-1 not-italic text-sm leading-relaxed text-slate">
                      {address.line1}
                      <br />
                      {address.line2 && (
                        <>
                          {address.line2}
                          <br />
                        </>
                      )}
                      {address.city}
                      {address.postalCode && ` ${address.postalCode}`}
                    </address>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>

        <Card className="lg:col-span-2">
          <CardHeader
            title="Orders"
            description={`Most recent first. ${countOf(data.orderCount, "order")} in total.`}
          />
          {data.recentOrders.length === 0 ? (
            <EmptyState
              title="No orders yet"
              description="This customer has an account but has not bought anything."
            />
          ) : (
            <ul className="flex flex-col">
              {data.recentOrders.map((order) => (
                <li key={order.id}>
                  <Link
                    href={`/admin/orders/${order.id}`}
                    className="flex flex-wrap items-center justify-between gap-3 border-b border-sand-soft/70 px-5 py-3 last:border-0 hover:bg-paper-sunk/60"
                  >
                    <div>
                      <p className="font-medium text-ink tabular">{order.orderNumber}</p>
                      <p className="text-xs text-slate">
                        {formatDate(order.placedAt)} · {countOf(order.itemCount, "item")}
                      </p>
                    </div>
                    <div className="flex items-center gap-3">
                      <Badge tone={STATUS_TONE[order.orderStatus]}>{order.orderStatus}</Badge>
                      <span className="tabular text-sm text-ink">
                        {formatMoney(order.total, order.currency)}
                      </span>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </>
  );
}
