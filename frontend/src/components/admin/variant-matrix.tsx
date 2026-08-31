"use client";

import { useMemo, useState } from "react";
import { adminApi, describeError } from "@/lib/admin-api";
import { ConfirmDialog } from "@/components/ui/dialog";
import { useToast } from "@/components/ui/toast";
import { Badge, Button, Card, CardHeader, Field, Input, cx } from "@/components/ui/primitives";
import { countOf } from "@/lib/format";
import type { ProductDetail, ProductVariant } from "@/types/admin";

/**
 * Size × colour stock grid.
 *
 * A shop owner thinks in "how many mediums in navy do I have", not in a list of
 * SKUs. The grid is built from the sizes and colours that actually exist on the
 * product, and every cell is one variant.
 */
export function VariantMatrix({
  product,
  onChanged,
}: {
  product: ProductDetail;
  onChanged: (updated: ProductDetail) => void;
}) {
  const toast = useToast();

  const [draft, setDraft] = useState<Record<string, number>>({});
  const [saving, setSaving] = useState(false);
  const [adding, setAdding] = useState(false);
  const [pendingRemove, setPendingRemove] = useState<ProductVariant | null>(null);
  const [removing, setRemoving] = useState(false);

  const [newSize, setNewSize] = useState("");
  const [newColor, setNewColor] = useState("");
  const [newHex, setNewHex] = useState("#1F2A44");
  const [newStock, setNewStock] = useState(0);
  const [newSku, setNewSku] = useState("");
  const [addError, setAddError] = useState<string | null>(null);

  const sizes = useMemo(
    () => [...new Set(product.variants.map((variant) => variant.size))],
    [product.variants],
  );

  const colors = useMemo(() => {
    const seen = new Map<string, string>();
    for (const variant of product.variants) {
      if (!seen.has(variant.color)) seen.set(variant.color, variant.colorHex);
    }
    return [...seen.entries()].map(([name, hex]) => ({ name, hex }));
  }, [product.variants]);

  const bySizeColor = useMemo(() => {
    const map = new Map<string, ProductVariant>();
    for (const variant of product.variants) {
      map.set(`${variant.size}|${variant.color}`, variant);
    }
    return map;
  }, [product.variants]);

  const dirty = Object.keys(draft).length > 0;

  function setCell(sku: string, value: number) {
    const variant = product.variants.find((candidate) => candidate.sku === sku);

    setDraft((current) => {
      const next = { ...current };

      // Editing a cell back to its saved value clears it from the draft, so
      // "unsaved changes" only ever means genuinely changed.
      if (variant && variant.stockQuantity === value) {
        delete next[sku];
      } else {
        next[sku] = value;
      }

      return next;
    });
  }

  async function saveStock() {
    setSaving(true);

    try {
      const result = await adminApi.put<{ updatedCount: number; problems: string[] }>(
        "/api/admin/products/stock",
        {
          updates: Object.entries(draft).map(([sku, stockQuantity]) => ({
            productId: product.id,
            sku,
            stockQuantity,
          })),
        },
      );

      if (result.problems.length > 0) {
        toast.fail(result.problems.join(" "));
      } else {
        toast.notify(`Stock updated for ${countOf(result.updatedCount, "variant")}.`);
      }

      setDraft({});
      onChanged(await adminApi.get<ProductDetail>(`/api/admin/products/${product.id}`));
    } catch (caught) {
      toast.fail(describeError(caught));
    } finally {
      setSaving(false);
    }
  }

  async function addVariant() {
    setAdding(true);
    setAddError(null);

    try {
      const updated = await adminApi.post<ProductDetail>(
        `/api/admin/products/${product.id}/variants`,
        {
          sku: newSku.trim().toUpperCase(),
          size: newSize.trim(),
          color: newColor.trim(),
          colorHex: newHex,
          stockQuantity: newStock,
          isActive: true,
        },
      );

      toast.notify(`${newSize} / ${newColor} added.`);
      setNewSize("");
      setNewColor("");
      setNewStock(0);
      setNewSku("");
      onChanged(updated);
    } catch (caught) {
      setAddError(describeError(caught));
    } finally {
      setAdding(false);
    }
  }

  async function confirmRemove() {
    if (!pendingRemove) return;
    setRemoving(true);

    try {
      const updated = await adminApi.delete<ProductDetail>(
        `/api/admin/products/${product.id}/variants/${encodeURIComponent(pendingRemove.sku)}`,
      );

      toast.notify(`${pendingRemove.size} / ${pendingRemove.color} withdrawn.`);
      setPendingRemove(null);
      onChanged(updated);
    } catch (caught) {
      toast.fail(describeError(caught));
    } finally {
      setRemoving(false);
    }
  }

  // Suggest a SKU so the field is rarely typed by hand.
  const suggestedSku =
    newSize && newColor
      ? `${product.slug.slice(0, 12).toUpperCase().replace(/-/g, "")}-${newColor
          .toUpperCase()
          .replace(/[^A-Z0-9]/g, "")}-${newSize.toUpperCase()}`
      : "";

  return (
    <>
      <Card>
        <CardHeader
          title="Sizes, colours and stock"
          description="Every cell is one sellable variant. Stock lives here, never on the product itself."
          actions={
            dirty ? (
              <>
                <Button variant="ghost" size="sm" onClick={() => setDraft({})} disabled={saving}>
                  Discard
                </Button>
                <Button size="sm" onClick={saveStock} disabled={saving}>
                  {saving ? "Saving…" : `Save ${countOf(Object.keys(draft).length, "change")}`}
                </Button>
              </>
            ) : undefined
          }
        />

        {product.variants.length === 0 ? (
          <p className="px-5 py-8 text-center text-sm text-slate">
            No sizes or colours yet. Add the first one below — a product with no
            variants cannot be bought.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-sand-soft">
                  <th
                    scope="col"
                    className="px-4 py-3 text-left text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate"
                  >
                    Colour
                  </th>
                  {sizes.map((size) => (
                    <th
                      key={size}
                      scope="col"
                      className="px-2 py-3 text-center text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate"
                    >
                      {size}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {colors.map((color) => (
                  <tr key={color.name} className="border-b border-sand-soft/70 last:border-0">
                    <th scope="row" className="px-4 py-2 text-left font-normal">
                      <span className="flex items-center gap-2">
                        <span
                          className="size-4 shrink-0 rounded-full border border-sand"
                          style={{ backgroundColor: color.hex }}
                          aria-hidden="true"
                        />
                        <span className="text-ink">{color.name}</span>
                      </span>
                    </th>

                    {sizes.map((size) => {
                      const variant = bySizeColor.get(`${size}|${color.name}`);

                      if (!variant) {
                        return (
                          <td key={size} className="px-2 py-2 text-center">
                            <span className="text-xs text-ash" title="This combination does not exist">
                              —
                            </span>
                          </td>
                        );
                      }

                      const value = draft[variant.sku] ?? variant.stockQuantity;
                      const changed = variant.sku in draft;

                      return (
                        <td key={size} className="px-2 py-2">
                          <div className="flex flex-col items-center gap-1">
                            <label className="sr-only" htmlFor={`stock-${variant.sku}`}>
                              Stock for {color.name} {size}
                            </label>
                            <input
                              id={`stock-${variant.sku}`}
                              type="number"
                              min={0}
                              value={value}
                              onChange={(event) =>
                                setCell(variant.sku, Math.max(0, Number(event.target.value) || 0))
                              }
                              className={cx(
                                "w-16 rounded-tailored border px-2 py-1 text-center text-sm tabular",
                                changed
                                  ? "border-indigo bg-indigo-soft/50 text-indigo"
                                  : value === 0
                                    ? "border-danger-soft bg-danger-soft/50 text-danger"
                                    : "border-sand-soft bg-chalk text-ink",
                              )}
                            />
                            <button
                              type="button"
                              onClick={() => setPendingRemove(variant)}
                              className="text-[0.625rem] uppercase tracking-wide text-ash hover:text-danger"
                            >
                              Withdraw
                            </button>
                          </div>
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="border-t border-sand-soft px-5 py-4">
          <h3 className="text-sm font-medium text-ink">Add a size or colour</h3>

          <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            <Field label="Size" htmlFor="new-size">
              <Input
                id="new-size"
                value={newSize}
                placeholder="M, 40R, 32"
                onChange={(event) => setNewSize(event.target.value)}
              />
            </Field>

            <Field label="Colour" htmlFor="new-color">
              <Input
                id="new-color"
                value={newColor}
                placeholder="Navy"
                onChange={(event) => setNewColor(event.target.value)}
              />
            </Field>

            <Field label="Colour swatch" htmlFor="new-hex">
              <input
                id="new-hex"
                type="color"
                value={newHex}
                onChange={(event) => setNewHex(event.target.value)}
                className="h-[38px] w-full rounded-tailored border border-sand-soft bg-chalk px-1"
              />
            </Field>

            <Field label="Opening stock" htmlFor="new-stock">
              <Input
                id="new-stock"
                type="number"
                min={0}
                className="tabular"
                value={newStock}
                onChange={(event) => setNewStock(Math.max(0, Number(event.target.value) || 0))}
              />
            </Field>

            <Field label="SKU" htmlFor="new-sku" hint="Unique across the whole catalogue.">
              <Input
                id="new-sku"
                value={newSku}
                placeholder={suggestedSku}
                onChange={(event) => setNewSku(event.target.value)}
                onFocus={() => {
                  if (!newSku && suggestedSku) setNewSku(suggestedSku);
                }}
              />
            </Field>
          </div>

          {addError && (
            <p role="alert" className="mt-3 text-sm text-danger">
              {addError}
            </p>
          )}

          <div className="mt-3">
            <Button
              size="sm"
              onClick={addVariant}
              disabled={adding || !newSize.trim() || !newColor.trim() || !(newSku || suggestedSku)}
            >
              {adding ? "Adding…" : "Add variant"}
            </Button>
          </div>
        </div>
      </Card>

      <ConfirmDialog
        open={pendingRemove !== null}
        onCancel={() => setPendingRemove(null)}
        onConfirm={confirmRemove}
        busy={removing}
        title="Withdraw this variant?"
        message={
          pendingRemove
            ? `${pendingRemove.size} in ${pendingRemove.color} will stop being sold and its stock set to zero. Past orders that reference it keep working.`
            : ""
        }
        confirmLabel="Withdraw variant"
      />
    </>
  );
}

/** Compact stock summary for the product header. */
export function StockSummary({ product }: { product: ProductDetail }) {
  const outOfStock = product.variants.filter((variant) => variant.stockQuantity === 0).length;

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Badge tone={product.totalStock === 0 ? "danger" : "neutral"}>
        {countOf(product.totalStock, "unit")} in stock
      </Badge>
      <Badge>{countOf(product.variants.length, "variant")}</Badge>
      {outOfStock > 0 && <Badge tone="warning">{outOfStock} out of stock</Badge>}
    </div>
  );
}
