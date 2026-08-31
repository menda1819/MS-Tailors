"use client";

import { use, useEffect } from "react";
import Link from "next/link";
import { useResource } from "@/components/admin/use-paged";
import { Button, ErrorState, Skeleton } from "@/components/ui/primitives";
import { formatDate, formatMoney } from "@/lib/format";
import type { Order } from "@/types/admin";

/**
 * Printable invoice.
 *
 * Laid out as a document rather than a screen: the admin chrome is hidden by
 * the .no-print rules, and @page in globals.css sets the margins, so Print to
 * PDF from the browser produces something worth handing to a customer.
 */
export default function InvoicePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data, loading, error, reload } = useResource<Order>(`/api/admin/orders/${id}`);

  useEffect(() => {
    if (data) document.title = `Invoice ${data.orderNumber} · MS Tailors`;
  }, [data]);

  if (loading) {
    return <Skeleton className="h-96 w-full" />;
  }

  if (error || !data) {
    return <ErrorState message={error ?? "That order could not be loaded."} onRetry={reload} />;
  }

  return (
    <div className="mx-auto max-w-3xl">
      <div className="no-print mb-5 flex flex-wrap items-center justify-between gap-3">
        <Link
          href={`/admin/orders/${id}`}
          className="text-sm text-indigo underline underline-offset-2"
        >
          Back to order
        </Link>
        <Button onClick={() => window.print()}>Print or save as PDF</Button>
      </div>

      <article className="print-sheet rounded-tailored border border-sand-soft bg-chalk p-8 sm:p-10">
        <header className="flex flex-wrap items-start justify-between gap-6 border-b border-sand-soft pb-6">
          <div>
            <div className="measure w-28" aria-hidden="true">
              {Array.from({ length: 14 }).map((_, index) => (
                <i key={index} />
              ))}
            </div>
            <h1 className="mt-3 font-display text-2xl leading-none text-ink">MS Tailors</h1>
            <p className="mt-1 text-[0.6875rem] font-semibold uppercase tracking-[0.16em] text-slate">
              Perfect Fit. Timeless Style.
            </p>
            <address className="mt-3 not-italic text-sm leading-relaxed text-slate">
              No. 28 Station Road
              <br />
              Panadura, Sri Lanka
            </address>
          </div>

          <div className="text-right">
            <p className="text-[0.6875rem] font-semibold uppercase tracking-[0.16em] text-slate">
              Invoice
            </p>
            <p className="mt-1 font-display text-xl text-ink tabular">{data.orderNumber}</p>
            <p className="mt-2 text-sm text-slate">{formatDate(data.placedAt)}</p>
            <p className="mt-1 text-sm text-slate">
              {data.paymentMethod === "CashOnDelivery" ? "Cash on delivery" : data.paymentMethod}
              {" · "}
              {data.paymentStatus}
            </p>
          </div>
        </header>

        <section className="grid gap-6 py-6 sm:grid-cols-2">
          <div>
            <h2 className="text-[0.6875rem] font-semibold uppercase tracking-[0.16em] text-slate">
              Billed to
            </h2>
            <p className="mt-2 text-sm font-medium text-ink">{data.contact.fullName}</p>
            <p className="text-sm text-slate">{data.contact.email}</p>
            <p className="text-sm text-slate">{data.contact.phone}</p>
          </div>

          <div>
            <h2 className="text-[0.6875rem] font-semibold uppercase tracking-[0.16em] text-slate">
              Delivered to
            </h2>
            <address className="mt-2 not-italic text-sm leading-relaxed text-ink">
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
          </div>
        </section>

        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-y border-sand-soft">
              <th scope="col" className="py-2.5 text-left text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                Item
              </th>
              <th scope="col" className="py-2.5 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                Qty
              </th>
              <th scope="col" className="py-2.5 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                Unit
              </th>
              <th scope="col" className="py-2.5 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                Amount
              </th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((item) => (
              <tr key={item.variantSku} className="border-b border-sand-soft/70">
                <td className="py-3 pr-4">
                  <p className="text-ink">{item.productName}</p>
                  <p className="text-xs text-slate">
                    {item.size} · {item.color}
                  </p>
                </td>
                <td className="py-3 text-right tabular">{item.quantity}</td>
                <td className="py-3 text-right tabular">{formatMoney(item.unitPrice)}</td>
                <td className="py-3 text-right tabular">{formatMoney(item.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <dl className="ml-auto mt-5 flex w-full max-w-xs flex-col gap-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-slate">Subtotal</dt>
            <dd className="tabular text-ink">{formatMoney(data.subtotal)}</dd>
          </div>
          {data.discountAmount > 0 && (
            <div className="flex justify-between">
              <dt className="text-slate">
                Discount{data.couponCode ? ` (${data.couponCode})` : ""}
              </dt>
              <dd className="tabular text-ink">−{formatMoney(data.discountAmount)}</dd>
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

        <footer className="mt-8 border-t border-sand-soft pt-5 text-xs leading-relaxed text-slate">
          <p>Thank you for your custom.</p>
          <p className="mt-1">
            Questions about this order? Quote {data.orderNumber} when you get in touch.
          </p>
        </footer>
      </article>
    </div>
  );
}
