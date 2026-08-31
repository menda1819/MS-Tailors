"use client";

import { ProductForm } from "@/components/admin/product-form";
import { PageHeader } from "@/components/ui/primitives";

export default function NewProductPage() {
  return (
    <>
      <PageHeader
        eyebrow="Catalogue"
        title="New product"
        description="Save the details first, then add the sizes and colours it is sold in."
      />
      <ProductForm />
    </>
  );
}
