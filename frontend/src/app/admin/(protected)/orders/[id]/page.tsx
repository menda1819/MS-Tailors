"use client";

import { use, useState } from "react";
import Link from "next/link";
import { adminApi, describeError } from "@/lib/admin-api";
import { useResource } from "@/components/admin/use-paged";
import { ConfirmDialog } from "@/components/ui/dialog";
import { useToast } from "@/components/ui/toast";
import { PAYMENT_TONE, STATUS_TONE } from "@/app/admin/(protected)/orders/page";
import {
  Badge,
  Button,
  Card,
  CardHeader,
  ErrorState,
  Field,
  Input,
  PageHeader,
  Select,
  Skeleton,
  Textarea,
} from "@/components/ui/primitives";
import { countOf, formatDateTime, formatMoney } from "@/lib/format";
import type { Order, OrderStatus } from "@/types/admin";

/**
 * Which statuses can follow the current one.
 *
 * Mirrors the rule the API enforces: cancelled and returned are terminal, and a
 * delivered order can only be returned. Showing an option the server will
 * refuse is worse than not offering it.
 */
function nextStatuses(current: OrderStatus): OrderStatus[] {
  if (current === "Cancelled" || current === "Returned") return [];
  if (current === "Delivered") return ["Returned"];

  return (
    ["Confirmed", "Processing", "Packed", "Shipped", "Delivered", "Cancelled"] as OrderStatus[]
  ).filter((status) => status !== current);
}

export default function OrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const toast = useToast();
  const { data, loading, error, reload, setData } = useResource<Order>(`/api/admin/orders/${id}`);

  const [status, setStatus] = useState<OrderStatus | "">("");
  const [note, setNote] = useState("");
  const [tracking, setTracking] = useState("");
  const [confirming, setConfirming] = useState(false);
  const [saving, setSaving] = useState(false);

  async function applyStatus() {
    if (!status) return;
    setSaving(true);

    try {
      const updated = await adminApi.put<Order>(`/api/admin/orders/${id}/status`, {
        status,
        note: note || null,
        trackingNumber: tracking || null,
      });

      setData(updated);
      setStatus("");
      setNote("");
      setTracking("");
      setConfirming(false);
      toast.notify(`Order marked ${updated.orderStatus}.`);
    } catch (caught) {
      toast.fail(describeError(caught));
      setConfirming(false);
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <>
        <PageHeader eyebrow="Today" title="Order" />
        <div className="flex flex-col gap-4" aria-busy="true" aria-label="Loading order">
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      </>
    );
  }

  if (error || !data) {
    return (
      <>
        <PageHeader eyebrow="Today" title="Order" />
        <ErrorState message={error ?? "That order could not be loaded."} onRetry={reload} />
      </>
    );
  }

  const options = nextStatuses(data.orderStatus);
  const restoresStock = status === "Cancelled" || status === "Returned";

  return (
    <>
      <PageHeader
        eyebrow={
          <Link href="/admin/orders" className="text-indigo underline underline-offset-2">
            Orders
          </Link>
        }
        title={data.orderNumber}
        description={`Placed ${formatDateTime(data.placedAt)} · ${countOf(
          data.items.reduce((sum, item) => sum + item.quantity, 0),
          "item",
        )}`}
        actions={
          <Link
            href={`/admin/orders/${id}/invoice`}
            className="inline-flex items-center rounded-tailored border border-sand bg-chalk px-4 py-2.5 text-sm font-medium text-ink hover:bg-paper-sunk"
          >
            Print invoice
          </Link>
        }
      />

      <div className="mb-4 flex flex-wrap gap-2">
        <Badge tone={STATUS_TONE[data.orderStatus]}>{data.orderStatus}</Badge>
        <Badge tone={PAYMENT_TONE[data.paymentStatus]}>Payment {data.paymentStatus}</Badge>
        <Badge>{data.paymentMethod === "CashOnDelivery" ? "Cash on delivery" : data.paymentMethod}</Badge>
        {data.trackingNumber && <Badge tone="accent">Tracking {data.trackingNumber}</Badge>}
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="flex flex-col gap-4 lg:col-span-2">
          <Card>
            <CardHeader title="Items" />
            <div className="overflow-x-auto">
              <table className="w-full border-collapse text-sm">
                <thead>
                  <tr className="border-b border-sand-soft">
                    <th scope="col" className="px-4 py-2.5 text-left text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                      Product
                    </th>
                    <th scope="col" className="px-4 py-2.5 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                      Qty
                    </th>
                    <th scope="col" className="px-4 py-2.5 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                      Unit
                    </th>
                    <th scope="col" className="px-4 py-2.5 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                      Total
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((item) => (
                    <tr key={item.variantSku} className="border-b border-sand-soft/70 last:border-0">
                      <td className="px-4 py-3">
                        <p className="text-ink">{item.productName}</p>
                        <p className="text-xs text-slate">
                          {item.size} · {item.color} · {item.variantSku}
                        </p>
                      </td>
                      <td className="px-4 py-3 text-right tabular">{item.quantity}</td>
                      <td className="px-4 py-3 text-right tabular">{formatMoney(item.unitPrice)}</td>
                      <td className="px-4 py-3 text-right tabular">{formatMoney(item.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <dl className="flex flex-col gap-2 border-t border-sand-soft px-4 py-4 text-sm">
              <div className="flex justify-between">
                <dt className="text-slate">Subtotal</dt>
                <dd className="tabular text-ink">{formatMoney(data.subtotal)}</dd>
              </div>
              {data.discountAmount > 0 && (
                <div className="flex justify-between">
                  <dt className="text-slate">
                    Discount{data.couponCode ? ` (${data.couponCode})` : ""}
                  </dt>
                  <dd className="tabular text-success">−{formatMoney(data.discountAmount)}</dd>
                </div>
              )}
              <div className="flex justify-between">
                <dt className="text-slate">Delivery</dt>
                <dd className="tabular text-ink">
                  {data.deliveryFee === 0 ? "Free" : formatMoney(data.deliveryFee)}
                </dd>
              </div>
              <div className="flex justify-between border-t border-sand-soft pt-2 text-base">
                <dt className="font-medium text-ink">Total</dt>
                <dd className="tabular font-medium text-ink">
                  {formatMoney(data.total, data.currency)}
                </dd>
              </div>
            </dl>
          </Card>

          <Card>
            <CardHeader title="History" description="Every status change, with who made it." />
            <ol className="flex flex-col">
              {data.statusHistory.map((entry, index) => (
                <li
                  key={index}
                  className="flex flex-wrap items-baseline justify-between gap-2 border-b border-sand-soft/70 px-5 py-3 last:border-0"
                >
                  <div>
                    <Badge tone={STATUS_TONE[entry.status]}>{entry.status}</Badge>
                    {entry.note && <p className="mt-1 text-sm text-slate">{entry.note}</p>}
                  </div>
                  <div className="text-right">
                    <p className="text-xs text-slate">{formatDateTime(entry.changedAt)}</p>
                    <p className="text-xs text-ash">{entry.changedBy}</p>
                  </div>
                </li>
              ))}
            </ol>
          </Card>
        </div>

        <div className="flex flex-col gap-4">
          <Card>
            <CardHeader title="Customer" />
            <div className="flex flex-col gap-1 px-5 py-4 text-sm">
              <p className="font-medium text-ink">{data.contact.fullName}</p>
              <a href={`mailto:${data.contact.email}`} className="text-indigo underline underline-offset-2">
                {data.contact.email}
              </a>
              <a href={`tel:${data.contact.phone}`} className="text-indigo underline underline-offset-2">
                {data.contact.phone}
              </a>
              {data.customerId ? (
                <Link
                  href={`/admin/customers/${data.customerId}`}
                  className="mt-2 text-[0.8125rem] text-indigo underline underline-offset-2"
                >
                  View account
                </Link>
              ) : (
                <p className="mt-2 text-xs text-slate">Guest checkout — no account.</p>
              )}
            </div>
          </Card>

          <Card>
            <CardHeader title="Deliver to" />
            <address className="not-italic px-5 py-4 text-sm leading-relaxed text-ink">
              {data.shippingAddress.line1}
              <br />
              {data.shippingAddress.line2 && (
                <>
                  {data.shippingAddress.line2}
                  <br />
                </>
              )}
              {data.shippingAddress.city}
              {data.shippingAddress.postalCode && ` ${data.shippingAddress.postalCode}`}
            </address>
            {data.specialInstructions && (
              <div className="border-t border-sand-soft px-5 py-4">
                <p className="text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                  Instructions
                </p>
                <p className="mt-1 text-sm text-ink">{data.specialInstructions}</p>
              </div>
            )}
          </Card>

          <Card>
            <CardHeader title="Update status" />
            <div className="flex flex-col gap-3 px-5 py-4">
              {options.length === 0 ? (
                <p className="text-sm text-slate">
                  This order is {data.orderStatus.toLowerCase()} and cannot change again.
                </p>
              ) : (
                <>
                  <Field label="New status" htmlFor="new-status">
                    <Select
                      id="new-status"
                      value={status}
                      onChange={(event) => setStatus(event.target.value as OrderStatus)}
                    >
                      <option value="">Choose…</option>
                      {options.map((option) => (
                        <option key={option} value={option}>
                          {option}
                        </option>
                      ))}
                    </Select>
                  </Field>

                  {status === "Shipped" && (
                    <Field label="Tracking number" htmlFor="tracking">
                      <Input
                        id="tracking"
                        value={tracking}
                        onChange={(event) => setTracking(event.target.value)}
                      />
                    </Field>
                  )}

                  <Field label="Note" htmlFor="note" hint="Kept on the order history. Optional.">
                    <Textarea
                      id="note"
                      rows={2}
                      value={note}
                      onChange={(event) => setNote(event.target.value)}
                    />
                  </Field>

                  {restoresStock && (
                    <p className="rounded-tailored bg-warning-soft px-3 py-2 text-xs text-warning">
                      Stock for every item on this order will be returned to the shop.
                    </p>
                  )}

                  <Button disabled={!status || saving} onClick={() => setConfirming(true)}>
                    Update status
                  </Button>
                </>
              )}
            </div>
          </Card>
        </div>
      </div>

      <ConfirmDialog
        open={confirming}
        onCancel={() => setConfirming(false)}
        onConfirm={applyStatus}
        busy={saving}
        tone={restoresStock ? "danger" : "primary"}
        title={`Mark this order ${status}?`}
        message={
          restoresStock
            ? `${data.orderNumber} will be marked ${status.toLowerCase()}, and the stock for every item on it returned to the shop. This cannot be undone.`
            : `${data.orderNumber} will be marked ${status.toLowerCase()} and the change recorded against your account.`
        }
        confirmLabel={`Mark ${status}`}
      />
    </>
  );
}
