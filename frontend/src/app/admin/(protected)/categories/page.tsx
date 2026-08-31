"use client";

import { useEffect, useState } from "react";
import { adminApi, describeError } from "@/lib/admin-api";
import { ResourceManager, type FieldDef } from "@/components/admin/resource-manager";
import { Thumb } from "@/components/admin/image-field";
import { Badge } from "@/components/ui/primitives";
import type { Category } from "@/types/admin";

/**
 * Categories nest one level deep. The parent picker is populated from the
 * existing top-level categories, so a child can never be nested under a child.
 */
export default function CategoriesPage() {
  const [parents, setParents] = useState<{ value: string; label: string }[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const all = await adminApi.get<Category[]>("/api/admin/categories/all");
        setParents(
          all
            .filter((category) => category.parentCategoryId === null)
            .map((category) => ({ value: category.id, label: category.name })),
        );
      } catch (caught) {
        setLoadError(describeError(caught));
      }
    })();
  }, []);

  const fields: FieldDef[] = [
    { name: "name", label: "Category name", kind: "text", required: true, span: "half" },
    { name: "slug", label: "URL slug", kind: "slug", required: true, span: "half", derivedFrom: "name" },
    {
      name: "parentCategoryId",
      label: "Parent category",
      kind: "select",
      span: "half",
      options: parents,
      hint: loadError ?? "Leave blank to make this a top-level category.",
    },
    { name: "displayOrder", label: "Display order", kind: "number", span: "half" },
    { name: "imageUrl", label: "Image", kind: "image", folder: "categories" },
    { name: "isActive", label: "Visible in the shop", kind: "checkbox" },
  ];

  return (
    <ResourceManager<Category>
      eyebrow="Catalogue"
      title="Categories"
      description="How products are grouped in the shop. One level of nesting: a top-level category and its children."
      endpoint="/api/admin/categories"
      rowKey={(category) => category.id}
      itemNoun="category"
      itemNounPlural="categories"
      describeRow={(category) => category.name}
      emptyTitle="No categories yet"
      emptyDescription="Create categories so customers can browse the shop by what they are looking for."
      pageSize={100}
      columns={[
        { key: "image", header: "", render: (category) => <Thumb url={category.imageUrl} alt={category.name} /> },
        {
          key: "name",
          header: "Category",
          render: (category) => (
            <div className="flex items-center gap-2">
              <span className="font-medium text-ink">{category.name}</span>
              {category.parentCategoryId === null && <Badge tone="accent">Top level</Badge>}
            </div>
          ),
        },
        {
          key: "slug",
          header: "Slug",
          secondary: true,
          render: (category) => <code className="text-xs text-slate">/{category.slug}</code>,
        },
        { key: "order", header: "Order", align: "right", render: (category) => category.displayOrder },
      ]}
      fields={fields}
      toForm={(category) => ({
        name: category.name,
        slug: category.slug,
        parentCategoryId: category.parentCategoryId ?? "",
        imageUrl: category.imageUrl,
        displayOrder: category.displayOrder,
        isActive: true,
      })}
      emptyForm={{
        name: "",
        slug: "",
        parentCategoryId: "",
        imageUrl: "",
        displayOrder: 0,
        isActive: true,
      }}
      toBody={(values) => ({
        ...values,
        parentCategoryId: values.parentCategoryId || null,
      })}
    />
  );
}
