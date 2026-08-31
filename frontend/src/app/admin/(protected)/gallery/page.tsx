"use client";

import { ResourceManager, type FieldDef } from "@/components/admin/resource-manager";
import { Thumb } from "@/components/admin/image-field";
import { Badge } from "@/components/ui/primitives";
import type { GalleryItem } from "@/types/admin";

const CATEGORIES = [
  { value: "Suits", label: "Suits" },
  { value: "Shirts", label: "Shirts" },
  { value: "Trousers", label: "Trousers" },
  { value: "WeddingWear", label: "Wedding Wear" },
  { value: "Alterations", label: "Alterations" },
  { value: "Uniforms", label: "Uniforms" },
  { value: "TailoringWork", label: "Tailoring Work" },
  { value: "Store", label: "Store" },
];

const FIELDS: FieldDef[] = [
  { name: "imageUrl", label: "Photograph", kind: "image", folder: "gallery", required: true },
  {
    name: "altText",
    label: "Alt text",
    kind: "text",
    required: true,
    hint: "Describe the photograph for someone who cannot see it. Required — screen readers depend on it.",
  },
  { name: "caption", label: "Caption", kind: "text", hint: "Shown beneath the photograph. Optional." },
  { name: "category", label: "Category", kind: "select", required: true, span: "half", options: CATEGORIES },
  { name: "displayOrder", label: "Display order", kind: "number", span: "half" },
  { name: "isActive", label: "Visible in the gallery", kind: "checkbox" },
];

export default function GalleryPage() {
  return (
    <ResourceManager<GalleryItem>
      eyebrow="Content"
      title="Gallery"
      description="Photographs of finished work. This is the strongest thing on the site for winning bespoke customers."
      endpoint="/api/admin/gallery"
      rowKey={(item) => item.id}
      itemNoun="photograph"
      itemNounPlural="photographs"
      describeRow={(item) => item.caption || item.altText}
      emptyTitle="No photographs yet"
      emptyDescription="Add photographs of finished suits, shirts and alterations. Customers judge a tailor by the work."
      pageSize={24}
      columns={[
        { key: "image", header: "", render: (item) => <Thumb url={item.imageUrl} alt={item.altText} /> },
        {
          key: "caption",
          header: "Photograph",
          render: (item) => (
            <div className="min-w-0">
              <p className="truncate text-ink">{item.caption || "Untitled"}</p>
              <p className="truncate text-xs text-slate">{item.altText}</p>
            </div>
          ),
        },
        {
          key: "category",
          header: "Category",
          secondary: true,
          render: (item) => <Badge>{item.categoryLabel}</Badge>,
        },
        { key: "order", header: "Order", align: "right", render: (item) => item.displayOrder },
      ]}
      fields={FIELDS}
      toForm={(item) => ({
        imageUrl: item.imageUrl,
        thumbnailUrl: item.thumbnailUrl,
        caption: item.caption,
        altText: item.altText,
        category: item.category,
        displayOrder: item.displayOrder,
        isActive: true,
      })}
      emptyForm={{
        imageUrl: "",
        thumbnailUrl: "",
        caption: "",
        altText: "",
        category: "Suits",
        displayOrder: 0,
        isActive: true,
      }}
      toBody={(values) => ({ ...values, thumbnailUrl: values.thumbnailUrl || values.imageUrl })}
    />
  );
}
