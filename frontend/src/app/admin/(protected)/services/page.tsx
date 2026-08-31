"use client";

import { ResourceManager, type FieldDef } from "@/components/admin/resource-manager";
import { Thumb } from "@/components/admin/image-field";
import type { ServiceDetail } from "@/types/admin";

const FIELDS: FieldDef[] = [
  { name: "name", label: "Service name", kind: "text", required: true, span: "half" },
  { name: "slug", label: "URL slug", kind: "slug", required: true, span: "half", derivedFrom: "name" },
  {
    name: "category",
    label: "Category",
    kind: "select",
    required: true,
    span: "half",
    options: [
      { value: "Suits", label: "Suits" },
      { value: "Shirts", label: "Shirts" },
      { value: "Trousers", label: "Trousers" },
      { value: "Uniforms", label: "Uniforms" },
      { value: "Kidswear", label: "Kidswear" },
      { value: "Rental", label: "Rental" },
      { value: "Alterations", label: "Alterations" },
    ],
  },
  { name: "displayOrder", label: "Display order", kind: "number", span: "half" },
  { name: "shortDescription", label: "Short description", kind: "textarea", required: true, rows: 2, hint: "One or two lines, shown on the services grid." },
  { name: "longDescription", label: "Full description", kind: "textarea", required: true, rows: 8 },
  {
    name: "bulletPoints",
    label: "What's included",
    kind: "lines",
    rows: 6,
    hint: "One point per line. These appear as a list on the service page.",
  },
  { name: "imageUrl", label: "Image", kind: "image", folder: "services" },
  { name: "isActive", label: "Visible on the site", kind: "checkbox" },
];

export default function ServicesPage() {
  return (
    <ResourceManager<ServiceDetail>
      eyebrow="Content"
      title="Tailoring services"
      description="These pages exist to bring in inquiries and appointments, so they carry no prices."
      endpoint="/api/admin/services"
      rowKey={(service) => service.id}
      itemNoun="service"
      itemNounPlural="services"
      describeRow={(service) => service.name}
      emptyTitle="No services listed"
      emptyDescription="Add the tailoring work you offer so customers can read about it and get in touch."
      columns={[
        {
          key: "image",
          header: "",
          render: (service) => <Thumb url={service.imageUrl} alt={service.name} />,
        },
        {
          key: "name",
          header: "Service",
          render: (service) => (
            <div className="min-w-0">
              <p className="truncate font-medium text-ink">{service.name}</p>
              <p className="truncate text-xs text-slate">{service.shortDescription}</p>
            </div>
          ),
        },
        { key: "category", header: "Category", secondary: true, render: (service) => service.category },
        { key: "order", header: "Order", align: "right", render: (service) => service.displayOrder },
      ]}
      fields={FIELDS}
      toForm={(service) => ({
        name: service.name,
        slug: service.slug,
        category: service.category,
        shortDescription: service.shortDescription,
        longDescription: service.longDescription,
        imageUrl: service.imageUrl,
        bulletPoints: service.bulletPoints,
        displayOrder: service.displayOrder,
        isActive: true,
      })}
      emptyForm={{
        name: "",
        slug: "",
        category: "",
        shortDescription: "",
        longDescription: "",
        imageUrl: "",
        bulletPoints: [],
        displayOrder: 0,
        isActive: true,
      }}
    />
  );
}
