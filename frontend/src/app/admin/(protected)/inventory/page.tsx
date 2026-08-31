"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { adminApi, describeError } from "@/lib/admin-api";
import { ConfirmDialog } from "@/components/ui/dialog";
import { useToast } from "@/components/ui/toast";
import {
  Badge,
  Button,
  Card,
  EmptyState,
  ErrorState,
  Field,
  Input,
  PageHeader,
  Select,
  Skeleton,
  cx,
} from "@/components/ui/primitives";
import { countOf } from "@/lib/format";
import type { BulkStockResult, LowStockVariant } from "@/types/admin";

type View = "low" | "out";

/**
 * Low-stock and out-of-stock views, with in-place bulk editing.
 *
 * The whole point of this screen is a stock-take: walk the shelves, type the
 * real numbers in, save once. Editing them one product at a time would make
 * that tedious enough that it would not get done.
 */
export default function InventoryPage() {
  const toast = useToast();

  const [view, setView] = useState<View>("low");
  const [threshold, setThreshold] = useState(5);
  const [rows, setRows] = useState<LowStockVariant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [draft, setDraft] = useState<Record<string, number>>({});
  const [confirming, setConfirming] = useState(false);
  const [saving, setSaving] = useState(false);

  const effectiveThreshold = view === "out" ? 0 : threshold;

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await adminApi.get<LowStockVariant[]>(
        `/api/admin/inventory/low-stock?threshold=${effectiveThreshold}`,
      );

      // The endpoint returns everything at or below the threshold, so the
      // out-of-stock view filters to exactly zero.
      setRows(view === "out" ? data.filter((row) => row.stockQuantity === 0) : data);
      setDraft({});
    } catch (caught) {
      setError(describeError(caught));
      setRows([]);
    } finally {
      setLoading(false);
    }
  }, [effectiveThreshold, view]);

  useEffect(() => {
    // Data fetch on mount and whenever the view or threshold changes.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const changes = useMemo(() => Object.entries(draft), [draft]);
  const dirty = changes.length > 0;

  function setCell(row: LowStockVariant, value: number) {
    const key = `${row.productId}|${row.sku}`;

    setDraft((current) => {
      const next = { ...current };
      if (row.stockQuantity === value) delete next[key];
      else next[key] = value;
      return next;
    });
  }

  async function save() {
    setSaving(true);

    try {
      const result = await adminApi.put<BulkStockResult>("/api/admin/products/stock", {
        updates: changes.map(([key, stockQuantity]) => {
          const [productId, sku] = key.split("|");
          return { productId, sku, stockQuantity };
        }),
      });

      if (result.problems.length > 0) {
        // Partial success is reported rather than swallowed — some rows may
        // have been renamed since the page loaded.
        toast.fail(
          `${countOf(result.updatedCount, "variant")} updated. ${result.problems.join(" ")}`,
        );
      } else {
        toast.notify(`Stock updated for ${countOf(result.updatedCount, "variant")}.`);
      }

      setConfirming(false);
      await load();
    } catch (caught) {
      toast.fail(describeError(caught));
      setConfirming(false);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Today"
        title="Inventory"
        description="Everything running low or already gone. Type the real counts in and save them together."
        actions={
          dirty ? (
            <>
              <Button variant="ghost" onClick={() => setDraft({})} disabled={saving}>
                Discard
              </Button>
              <Button onClick={() => setConfirming(true)} disabled={saving}>
                Save {countOf(changes.length, "change")}
              </Button>
            </>
          ) : (
            <Button variant="secondary" onClick={load}>
              Refresh
            </Button>
          )
        }
      />

      <Card>
        <div className="flex flex-wrap items-end gap-3 border-b border-sand-soft px-4 py-3">
          <Field label="View" htmlFor="view" className="min-w-48">
            <Select
              id="view"
              value={view}
              onChange={(event) => setView(event.target.value as View)}
            >
              <option value="low">Low stock</option>
              <option value="out">Out of stock</option>
            </Select>
          </Field>

          {view === "low" && (
            <Field
              label="At or below"
              htmlFor="threshold"
              className="min-w-32"
              hint="Units remaining"
            >
              <Input
                id="threshold"
                type="number"
                min={1}
                max={1000}
                className="tabular"
                value={threshold}
                onChange={(event) => setThreshold(Math.max(1, Number(event.target.value) || 1))}
              />
            </Field>
          )}
        </div>

        {loading ? (
          <div className="flex flex-col gap-3 p-5" aria-busy="true" aria-label="Loading inventory">
            {Array.from({ length: 8 }).map((_, index) => (
              <Skeleton key={index} className="h-11 w-full" />
            ))}
          </div>
        ) : error ? (
          <div className="p-5">
            <ErrorState message={error} onRetry={load} />
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            title={view === "out" ? "Nothing is out of stock" : "Stock is healthy"}
            description={
              view === "out"
                ? "Every size and colour currently has at least one unit on the shelf."
                : `No variant is down to ${threshold} units or fewer. Raise the threshold to look further ahead.`
            }
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-sand-soft">
                  <th scope="col" className="px-4 py-3 text-left text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                    Product
                  </th>
                  <th scope="col" className="px-4 py-3 text-left text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                    Variant
                  </th>
                  <th scope="col" className="hidden px-4 py-3 text-left text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate lg:table-cell">
                    SKU
                  </th>
                  <th scope="col" className="px-4 py-3 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                    On hand
                  </th>
                  <th scope="col" className="px-4 py-3 text-right text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate">
                    New count
                  </th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const key = `${row.productId}|${row.sku}`;
                  const value = draft[key] ?? row.stockQuantity;
                  const changed = key in draft;

                  return (
                    <tr key={key} className="border-b border-sand-soft/70 last:border-0">
                      <td className="px-4 py-3">
                        <Link
                          href={`/admin/products/${row.productId}`}
                          className="text-indigo underline underline-offset-2"
                        >
                          {row.productName}
                        </Link>
                      </td>
                      <td className="px-4 py-3 text-slate">
                        {row.size} · {row.color}
                      </td>
                      <td className="hidden px-4 py-3 lg:table-cell">
                        <code className="text-xs text-ash">{row.sku}</code>
                      </td>
                      <td className="px-4 py-3 text-right">
                        {row.stockQuantity === 0 ? (
                          <Badge tone="danger">Out</Badge>
                        ) : (
                          <Badge tone="warning">{row.stockQuantity}</Badge>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <label className="sr-only" htmlFor={`stock-${key}`}>
                          New count for {row.productName} {row.size} {row.color}
                        </label>
                        <input
                          id={`stock-${key}`}
                          type="number"
                          min={0}
                          value={value}
                          onChange={(event) =>
                            setCell(row, Math.max(0, Number(event.target.value) || 0))
                          }
                          className={cx(
                            "w-20 rounded-tailored border px-2 py-1 text-right text-sm tabular",
                            changed
                              ? "border-indigo bg-indigo-soft/50 text-indigo"
                              : "border-sand-soft bg-chalk text-ink",
                          )}
                        />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {rows.length > 0 && (
          <p className="border-t border-sand-soft px-4 py-3 text-[0.8125rem] text-slate">
            {countOf(rows.length, "variant")} shown.
            {dirty && ` ${countOf(changes.length, "change")} not yet saved.`}
          </p>
        )}
      </Card>

      <ConfirmDialog
        open={confirming}
        onCancel={() => setConfirming(false)}
        onConfirm={save}
        busy={saving}
        tone="primary"
        title="Save these stock counts?"
        message={`${countOf(changes.length, "variant")} will be set to the counts you have typed. This replaces the current figures rather than adding to them.`}
        confirmLabel="Save stock"
      />
    </>
  );
}
