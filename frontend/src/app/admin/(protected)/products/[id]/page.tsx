"use client";

import { use } from "react";
import Link from "next/link";
import { useResource } from "@/components/admin/use-paged";
import { ProductForm } from "@/components/admin/product-form";
import { StockSummary, VariantMatrix } from "@/components/admin/variant-matrix";
import { ErrorState, PageHeader, Skeleton } from "@/components/ui/primitives";
import type { ProductDetail } from "@/types/admin";

export default function EditProductPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data, loading, error, reload, setData } = useResource<ProductDetail>(
    `/api/admin/products/${id}`,
  );

  if (loading) {
    return (
      <>
        <PageHeader eyebrow="Catalogue" title="Product" />
        <div className="flex flex-col gap-4" aria-busy="true" aria-label="Loading product">
          <Skeleton className="h-64 w-full" />
          <Skeleton className="h-48 w-full" />
        </div>
      </>
    );
  }

  if (error || !data) {
    return (
      <>
        <PageHeader eyebrow="Catalogue" title="Product" />
        <ErrorState
          message={error ?? "That product could not be loaded."}
          onRetry={reload}
        />
      </>
    );
  }

  return (
    <>
      <PageHeader
        eyebrow={
          <Link href="/admin/products" className="text-indigo underline underline-offset-2">
            Products
          </Link>
        }
        title={data.name}
        description={`/product/${data.slug}`}
      />

      <div className="mb-4">
        <StockSummary product={data} />
      </div>

      <div className="flex flex-col gap-4">
        <VariantMatrix product={data} onChanged={setData} />
        <ProductForm product={data} />
      </div>
    </>
  );
}
