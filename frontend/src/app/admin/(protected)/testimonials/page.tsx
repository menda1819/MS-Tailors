"use client";

import { ResourceManager, type FieldDef } from "@/components/admin/resource-manager";
import type { Testimonial } from "@/types/admin";

const FIELDS: FieldDef[] = [
  { name: "customerName", label: "Customer name", kind: "text", required: true, span: "half" },
  { name: "customerTitle", label: "Title or occupation", kind: "text", span: "half", hint: "Optional, e.g. \"Groom, December 2025\"." },
  { name: "quote", label: "What they said", kind: "textarea", required: true, rows: 5 },
  {
    name: "rating",
    label: "Rating",
    kind: "select",
    required: true,
    span: "half",
    options: [5, 4, 3, 2, 1].map((value) => ({ value: String(value), label: `${value} star${value === 1 ? "" : "s"}` })),
  },
  { name: "displayOrder", label: "Display order", kind: "number", span: "half" },
  { name: "imageUrl", label: "Photograph", kind: "image", folder: "testimonials" },
  {
    name: "isApproved",
    label: "Approved for the website",
    kind: "checkbox",
    hint: "Unapproved testimonials are never shown publicly.",
  },
  { name: "isActive", label: "Active", kind: "checkbox" },
];

export default function TestimonialsPage() {
  return (
    <ResourceManager<Testimonial>
      eyebrow="Content"
      title="Testimonials"
      description="Nothing appears on the website until you tick Approved."
      endpoint="/api/admin/testimonials"
      rowKey={(item) => item.id}
      itemNoun="testimonial"
      itemNounPlural="testimonials"
      describeRow={(item) => `${item.customerName}: ${item.quote.slice(0, 40)}…`}
      emptyTitle="No testimonials yet"
      emptyDescription="Add what customers have said about their suits, shirts and alterations."
      columns={[
        {
          key: "customer",
          header: "Customer",
          render: (item) => (
            <div className="min-w-0">
              <p className="truncate font-medium text-ink">{item.customerName}</p>
              {item.customerTitle && <p className="truncate text-xs text-slate">{item.customerTitle}</p>}
            </div>
          ),
        },
        {
          key: "quote",
          header: "Quote",
          secondary: true,
          render: (item) => <p className="max-w-md truncate text-sm text-slate">{item.quote}</p>,
        },
        {
          key: "rating",
          header: "Rating",
          align: "right",
          render: (item) => <span className="tabular text-sm">{item.rating} / 5</span>,
        },
      ]}
      fields={FIELDS}
      toForm={(item) => ({
        customerName: item.customerName,
        customerTitle: item.customerTitle ?? "",
        quote: item.quote,
        rating: String(item.rating),
        imageUrl: item.imageUrl ?? "",
        isApproved: true,
        displayOrder: item.displayOrder,
        isActive: true,
      })}
      emptyForm={{
        customerName: "",
        customerTitle: "",
        quote: "",
        rating: "5",
        imageUrl: "",
        isApproved: false,
        displayOrder: 0,
        isActive: true,
      }}
      toBody={(values) => ({ ...values, rating: Number(values.rating) })}
    />
  );
}
