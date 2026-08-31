"use client";

import { useState } from "react";
import { adminApi, describeError } from "@/lib/admin-api";
import { usePaged } from "@/components/admin/use-paged";
import { DataTable, Pagination } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/dialog";
import { useToast } from "@/components/ui/toast";
import {
  Badge,
  Button,
  Card,
  Field,
  PageHeader,
  Select,
} from "@/components/ui/primitives";
import { formatDate, formatRelative } from "@/lib/format";
import type { Inquiry, InquiryStatus } from "@/types/admin";

const STATUS_TONE: Record<InquiryStatus, "accent" | "warning" | "neutral"> = {
  New: "accent",
  Contacted: "warning",
  Closed: "neutral",
};

export default function InquiriesPage() {
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [open, setOpen] = useState<Inquiry | null>(null);
  const [saving, setSaving] = useState(false);

  const { items, meta, loading, error, reload } = usePaged<Inquiry>("/api/admin/inquiries", {
    page,
    pageSize: 20,
    status: status || undefined,
  });

  async function setInquiryStatus(inquiry: Inquiry, next: InquiryStatus) {
    setSaving(true);

    try {
      await adminApi.put(`/api/admin/inquiries/${inquiry.id}/status`, { status: next });
      toast.notify(`Marked ${next.toLowerCase()}.`);
      setOpen(null);
      await reload();
    } catch (caught) {
      toast.fail(describeError(caught));
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Today"
        title="Inquiries"
        description="Everyone who has asked about tailoring through the website. Reply by phone, email or WhatsApp, then mark them contacted."
      />

      <Card>
        <div className="grid gap-3 border-b border-sand-soft px-4 py-3 sm:max-w-xs">
          <Field label="Status" htmlFor="status">
            <Select
              id="status"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
                setPage(1);
              }}
            >
              <option value="">All inquiries</option>
              <option value="New">New — not yet answered</option>
              <option value="Contacted">Contacted</option>
              <option value="Closed">Closed</option>
            </Select>
          </Field>
        </div>

        <DataTable
          columns={[
            {
              key: "name",
              header: "From",
              render: (inquiry) => (
                <div className="min-w-0">
                  <p className="truncate font-medium text-ink">{inquiry.name}</p>
                  <p className="truncate text-xs text-slate">{inquiry.phone}</p>
                </div>
              ),
            },
            {
              key: "service",
              header: "Wants",
              render: (inquiry) => <Badge>{inquiry.serviceNeededLabel}</Badge>,
            },
            {
              key: "message",
              header: "Message",
              secondary: true,
              render: (inquiry) => (
                <p className="max-w-md truncate text-sm text-slate">{inquiry.message}</p>
              ),
            },
            {
              key: "preferred",
              header: "Preferred date",
              secondary: true,
              render: (inquiry) => (
                <span className="text-sm text-slate">{formatDate(inquiry.preferredDate)}</span>
              ),
            },
            {
              key: "received",
              header: "Received",
              align: "right",
              render: (inquiry) => (
                <span className="text-sm text-slate">{formatRelative(inquiry.receivedAt)}</span>
              ),
            },
            {
              key: "status",
              header: "Status",
              render: (inquiry) => (
                <Badge tone={STATUS_TONE[inquiry.status]}>{inquiry.status}</Badge>
              ),
            },
          ]}
          rows={items}
          rowKey={(inquiry) => inquiry.id}
          onRowClick={setOpen}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyTitle={status ? `No ${status.toLowerCase()} inquiries` : "No inquiries yet"}
          emptyDescription={
            status
              ? "Clear the filter to see the rest of the inbox."
              : "When someone fills in the contact form on the website, it arrives here."
          }
        />

        <Pagination page={page} meta={meta} onPageChange={setPage} itemNoun="inquiries" />
      </Card>

      <Modal
        open={open !== null}
        onClose={() => setOpen(null)}
        title={open ? `${open.name} · ${open.serviceNeededLabel}` : ""}
        description={open ? `Received ${formatRelative(open.receivedAt)}` : undefined}
        footer={
          open && (
            <>
              <Button variant="secondary" onClick={() => setOpen(null)} disabled={saving}>
                Close
              </Button>
              {open.status !== "Contacted" && (
                <Button
                  variant="secondary"
                  onClick={() => setInquiryStatus(open, "Contacted")}
                  disabled={saving}
                >
                  Mark contacted
                </Button>
              )}
              {open.status !== "Closed" && (
                <Button onClick={() => setInquiryStatus(open, "Closed")} disabled={saving}>
                  Mark closed
                </Button>
              )}
            </>
          )
        }
      >
        {open && (
          <div className="flex flex-col gap-4 text-sm">
            <div className="flex flex-wrap gap-2">
              <Badge tone={STATUS_TONE[open.status]}>{open.status}</Badge>
              <Badge>{open.serviceNeededLabel}</Badge>
            </div>

            <div className="flex flex-wrap gap-4">
              <a href={`tel:${open.phone}`} className="text-indigo underline underline-offset-2">
                {open.phone}
              </a>
              <a href={`mailto:${open.email}`} className="text-indigo underline underline-offset-2">
                {open.email}
              </a>
              <a
                href={`https://wa.me/${open.phone.replace(/[^0-9]/g, "")}`}
                target="_blank"
                rel="noopener noreferrer"
                className="text-indigo underline underline-offset-2"
              >
                WhatsApp
              </a>
            </div>

            {open.preferredDate && (
              <p className="text-slate">
                Preferred date: <span className="text-ink">{formatDate(open.preferredDate)}</span>
              </p>
            )}

            <div>
              <p className="text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                Message
              </p>
              <p className="mt-2 whitespace-pre-wrap leading-relaxed text-ink">{open.message}</p>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}
