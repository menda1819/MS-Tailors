"use client";

import Link from "next/link";
import { useState } from "react";
import { usePaged } from "@/components/admin/use-paged";
import { DataTable, Pagination } from "@/components/admin/data-table";
import { Badge, Card, Field, Input, PageHeader, Select } from "@/components/ui/primitives";
import { formatDate, formatRelative } from "@/lib/format";
import type { AdminCustomer } from "@/types/admin";

export default function CustomersPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [role, setRole] = useState("");

  const { items, meta, loading, error, reload } = usePaged<AdminCustomer>(
    "/api/admin/customers",
    { page, pageSize: 20, search: search || undefined, role: role || undefined },
  );

  return (
    <>
      <PageHeader
        eyebrow="People"
        title="Customers"
        description="Registered accounts. Guest orders have no account and appear only under Orders."
      />

      <Card>
        <div className="grid gap-3 border-b border-sand-soft px-4 py-3 sm:grid-cols-3">
          <Field label="Search" htmlFor="search" className="sm:col-span-2">
            <Input
              id="search"
              type="search"
              placeholder="Name or email"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
            />
          </Field>

          <Field label="Role" htmlFor="role">
            <Select
              id="role"
              value={role}
              onChange={(event) => {
                setRole(event.target.value);
                setPage(1);
              }}
            >
              <option value="">Everyone</option>
              <option value="Customer">Customers</option>
              <option value="Admin">Staff</option>
            </Select>
          </Field>
        </div>

        <DataTable
          columns={[
            {
              key: "name",
              header: "Customer",
              render: (customer) => (
                <Link href={`/admin/customers/${customer.id}`} className="min-w-0 block">
                  <p className="truncate font-medium text-indigo underline underline-offset-2">
                    {customer.fullName}
                  </p>
                  <p className="truncate text-xs text-slate">{customer.email}</p>
                </Link>
              ),
            },
            { key: "phone", header: "Phone", secondary: true, render: (customer) => customer.phone },
            {
              key: "role",
              header: "Role",
              render: (customer) =>
                customer.role === "Admin" ? <Badge tone="accent">Staff</Badge> : <Badge>Customer</Badge>,
            },
            {
              key: "joined",
              header: "Joined",
              secondary: true,
              render: (customer) => (
                <span className="text-sm text-slate">{formatDate(customer.createdAt)}</span>
              ),
            },
            {
              key: "lastLogin",
              header: "Last seen",
              align: "right",
              render: (customer) => (
                <span className="text-sm text-slate">{formatRelative(customer.lastLoginAt)}</span>
              ),
            },
          ]}
          rows={items}
          rowKey={(customer) => customer.id}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyTitle={search || role ? "Nobody matches that" : "No accounts yet"}
          emptyDescription={
            search || role
              ? "Try a different name or email, or clear the filters."
              : "Customers who create an account will be listed here. Guest orders do not create accounts."
          }
        />

        <Pagination page={page} meta={meta} onPageChange={setPage} itemNoun="customers" />
      </Card>
    </>
  );
}
