"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { adminApi, describeError } from "@/lib/admin-api";
import { usePaged } from "@/components/admin/use-paged";
import { DataTable, Pagination } from "@/components/admin/data-table";
import { Thumb } from "@/components/admin/image-field";
import { ConfirmDialog } from "@/components/ui/dialog";
import { useToast } from "@/components/ui/toast";
import {
  Badge,
  Button,
  Card,
  Field,
  Input,
  PageHeader,
  Select,
} from "@/components/ui/primitives";
import { countOf, formatMoney } from "@/lib/format";
import type { BrandDetail, PagedResponse, ProductDetail } from "@/types/admin";

export default function ProductsPage() {
  const router = useRouter();
  const toast = useToast();

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [brandFilter, setBrandFilter] = useState("");
  const [stockFilter, setStockFilter] = useState("");
  const [brands, setBrands] = useState<BrandDetail[]>([]);

  const { items, meta, loading, error, reload } = usePaged<ProductDetail>(
    "/api/admin/products",
    { page, pageSize: 20 },
  );

  const [pendingDelete, setPendingDelete] = useState<ProductDetail | null>(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    void (async () => {
      try {
        const response = await adminApi.get<PagedResponse<BrandDetail>>(
          "/api/admin/brands?pageSize=100",
        );
        setBrands(response.items);
      } catch {
        // The filter is a convenience; the list still works without it.
      }
    })();
  }, []);

  // The admin product list is not a search endpoint, so filtering happens over
  // the loaded page. Deliberate: it keeps the server contract simple, and the
  // page size is bounded. Search across the whole catalogue uses the shop's
  // own /api/products endpoint.
  const visible = items.filter((product) => {
    const term = search.trim().toLowerCase();

    if (term && !product.name.toLowerCase().includes(term) && !product.slug.includes(term)) {
      return false;
    }

    if (brandFilter && product.brandId !== brandFilter) return false;
    if (stockFilter === "out" && product.totalStock > 0) return false;
    if (stockFilter === "low" && (product.totalStock === 0 || product.totalStock > 5)) return false;
    if (stockFilter === "in" && product.totalStock === 0) return false;

    return true;
  });

  async function confirmDelete() {
    if (!pendingDelete) return;
    setDeleting(true);

    try {
      await adminApi.delete(`/api/admin/products/${pendingDelete.id}`);
      toast.notify("Product deleted.");
      setPendingDelete(null);
      await reload();
    } catch (caught) {
      toast.fail(describeError(caught));
    } finally {
      setDeleting(false);
    }
  }

  const brandName = (id: string) => brands.find((brand) => brand.id === id)?.name ?? "—";

  return (
    <>
      <PageHeader
        eyebrow="Catalogue"
        title="Products"
        description="Ready-made stock sold through the shop. Stock lives on each size and colour, not on the product."
        actions={
          <>
            <Button variant="secondary" onClick={() => router.push("/admin/inventory")}>
              Bulk stock
            </Button>
            <Button onClick={() => router.push("/admin/products/new")}>New product</Button>
          </>
        }
      />

      <Card>
        <div className="grid gap-3 border-b border-sand-soft px-4 py-3 sm:grid-cols-3">
          <Field label="Search" htmlFor="search">
            <Input
              id="search"
              type="search"
              placeholder="Name or slug"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </Field>

          <Field label="Brand" htmlFor="brand">
            <Select
              id="brand"
              value={brandFilter}
              onChange={(event) => setBrandFilter(event.target.value)}
            >
              <option value="">All brands</option>
              {brands.map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Stock" htmlFor="stock">
            <Select
              id="stock"
              value={stockFilter}
              onChange={(event) => setStockFilter(event.target.value)}
            >
              <option value="">Any stock level</option>
              <option value="in">In stock</option>
              <option value="low">Low (1–5)</option>
              <option value="out">Out of stock</option>
            </Select>
          </Field>
        </div>

        <DataTable
          columns={[
            {
              key: "image",
              header: "",
              render: (product) => (
                <Thumb url={product.images[0]?.url ?? null} alt={product.name} />
              ),
            },
            {
              key: "name",
              header: "Product",
              render: (product) => (
                <div className="min-w-0">
                  <p className="truncate font-medium text-ink">{product.name}</p>
                  <p className="truncate text-xs text-slate">
                    {brandName(product.brandId)} · {countOf(product.variants.length, "variant")}
                  </p>
                </div>
              ),
            },
            {
              key: "price",
              header: "Price",
              align: "right",
              render: (product) => (
                <div>
                  <p className="text-sm text-ink">
                    {formatMoney(product.effectivePrice, product.currency)}
                  </p>
                  {product.isOnSale && (
                    <p className="text-xs text-ash line-through">
                      {formatMoney(product.basePrice, product.currency)}
                    </p>
                  )}
                </div>
              ),
            },
            {
              key: "stock",
              header: "Stock",
              align: "right",
              render: (product) =>
                product.totalStock === 0 ? (
                  <Badge tone="danger">Out of stock</Badge>
                ) : product.totalStock <= 5 ? (
                  <Badge tone="warning">{product.totalStock} left</Badge>
                ) : (
                  <span className="tabular text-sm text-slate">{product.totalStock}</span>
                ),
            },
            {
              key: "flags",
              header: "",
              secondary: true,
              render: (product) => (
                <div className="flex gap-1">
                  {product.isFeatured && <Badge tone="accent">Featured</Badge>}
                  {product.isNewArrival && <Badge>New</Badge>}
                </div>
              ),
            },
            {
              key: "actions",
              header: "",
              align: "right",
              render: (product) => (
                <div className="flex justify-end gap-1">
                  <Link
                    href={`/admin/products/${product.id}`}
                    className="rounded-tailored px-3 py-1.5 text-[0.8125rem] font-medium text-slate hover:bg-paper-sunk hover:text-ink"
                  >
                    Edit
                  </Link>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-danger hover:bg-danger-soft hover:text-danger"
                    onClick={() => setPendingDelete(product)}
                  >
                    Delete
                  </Button>
                </div>
              ),
            },
          ]}
          rows={visible}
          rowKey={(product) => product.id}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyTitle={
            items.length > 0 ? "Nothing matches those filters" : "No products yet"
          }
          emptyDescription={
            items.length > 0
              ? "Clear the search or filters to see the rest of the catalogue."
              : "Add your first product. You will need at least one brand and one category first."
          }
          emptyAction={
            items.length === 0 ? (
              <Button onClick={() => router.push("/admin/products/new")}>New product</Button>
            ) : undefined
          }
        />

        <Pagination page={page} meta={meta} onPageChange={setPage} itemNoun="products" />
      </Card>

      <ConfirmDialog
        open={pendingDelete !== null}
        onCancel={() => setPendingDelete(null)}
        onConfirm={confirmDelete}
        busy={deleting}
        title="Delete this product?"
        message={
          pendingDelete
            ? `"${pendingDelete.name}" will be removed from the shop. Past orders that contain it are not affected.`
            : ""
        }
        confirmLabel="Delete product"
      />
    </>
  );
}
