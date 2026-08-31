"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { usePaged } from "@/components/admin/use-paged";
import { DataTable, Pagination } from "@/components/admin/data-table";
import { Badge, Card, Field, PageHeader, Select } from "@/components/ui/primitives";
import { countOf, formatMoney, formatRelative } from "@/lib/format";
import type { OrderStatus, OrderSummary, PaymentStatus } from "@/types/admin";

export const STATUS_TONE: Record<OrderStatus, "neutral" | "accent" | "success" | "warning" | "danger"> = {
  New: "accent",
  Confirmed: "accent",
  Processing: "warning",
  Packed: "warning",
  Shipped: "accent",
  Delivered: "success",
  Cancelled: "danger",
  Returned: "danger",
};

export const PAYMENT_TONE: Record<PaymentStatus, "neutral" | "success" | "warning" | "danger"> = {
  Pending: "warning",
  Paid: "success",
  Failed: "danger",
  Refunded: "neutral",
};

const STATUSES: OrderStatus[] = [
  "New",
  "Confirmed",
  "Processing",
  "Packed",
  "Shipped",
  "Delivered",
  "Cancelled",
  "Returned",
];

function OrdersTable() {
  const params = useSearchParams();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string>(params.get("status") ?? "");
  const [paymentStatus, setPaymentStatus] = useState("");

  const { items, meta, loading, error, reload } = usePaged<OrderSummary>("/api/admin/orders", {
    page,
    pageSize: 20,
    status: status || undefined,
    paymentStatus: paymentStatus || undefined,
  });

  return (
    <Card>
      <div className="grid gap-3 border-b border-sand-soft px-4 py-3 sm:grid-cols-2 lg:grid-cols-4">
        <Field label="Fulfilment status" htmlFor="status">
          <Select
            id="status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
          >
            <option value="">All orders</option>
            {STATUSES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </Field>

        <Field label="Payment status" htmlFor="payment">
          <Select
            id="payment"
            value={paymentStatus}
            onChange={(event) => {
              setPaymentStatus(event.target.value);
              setPage(1);
            }}
          >
            <option value="">Any payment state</option>
            {(["Pending", "Paid", "Failed", "Refunded"] as PaymentStatus[]).map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </Field>
      </div>

      <DataTable
        columns={[
          {
            key: "orderNumber",
            header: "Order",
            render: (order) => (
              <Link
                href={`/admin/orders/${order.id}`}
                className="font-medium text-indigo underline underline-offset-2"
              >
                {order.orderNumber}
              </Link>
            ),
          },
          {
            key: "placed",
            header: "Placed",
            render: (order) => (
              <span className="text-sm text-slate">{formatRelative(order.placedAt)}</span>
            ),
          },
          {
            key: "items",
            header: "Items",
            secondary: true,
            align: "right",
            render: (order) => countOf(order.itemCount, "item"),
          },
          {
            key: "total",
            header: "Total",
            align: "right",
            render: (order) => formatMoney(order.total, order.currency),
          },
          {
            key: "payment",
            header: "Payment",
            secondary: true,
            render: (order) => (
              <Badge tone={PAYMENT_TONE[order.paymentStatus]}>{order.paymentStatus}</Badge>
            ),
          },
          {
            key: "status",
            header: "Status",
            render: (order) => (
              <Badge tone={STATUS_TONE[order.orderStatus]}>{order.orderStatus}</Badge>
            ),
          },
        ]}
        rows={items}
        rowKey={(order) => order.id}
        loading={loading}
        error={error}
        onRetry={reload}
        emptyTitle={status || paymentStatus ? "No orders match those filters" : "No orders yet"}
        emptyDescription={
          status || paymentStatus
            ? "Clear the filters to see every order."
            : "Orders placed in the shop will appear here, newest first."
        }
      />

      <Pagination page={page} meta={meta} onPageChange={setPage} itemNoun="orders" />
    </Card>
  );
}

export default function OrdersPage() {
  return (
    <>
      <PageHeader
        eyebrow="Today"
        title="Orders"
        description="Every order placed in the shop. Open one to change its status or print an invoice."
      />
      <Suspense fallback={null}>
        <OrdersTable />
      </Suspense>
    </>
  );
}
