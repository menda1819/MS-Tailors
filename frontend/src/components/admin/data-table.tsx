"use client";

import type { ReactNode } from "react";
import {
  Button,
  EmptyState,
  ErrorState,
  Skeleton,
  cx,
} from "@/components/ui/primitives";
import type { PagedResponse } from "@/types/admin";

export interface Column<T> {
  key: string;
  header: string;
  /** Right-align numeric columns so digits line up down the page. */
  align?: "left" | "right";
  /** Hidden below the large breakpoint — for columns that are nice, not vital. */
  secondary?: boolean;
  render: (row: T) => ReactNode;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  loading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  emptyTitle: string;
  emptyDescription: string;
  emptyAction?: ReactNode;
  onRowClick?: (row: T) => void;
}

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading,
  error,
  onRetry,
  emptyTitle,
  emptyDescription,
  emptyAction,
  onRowClick,
}: DataTableProps<T>) {
  if (error) {
    return (
      <div className="p-5">
        <ErrorState message={error} onRetry={onRetry} />
      </div>
    );
  }

  if (loading) {
    return (
      <div className="flex flex-col gap-3 p-5" aria-busy="true" aria-label="Loading">
        {Array.from({ length: 6 }).map((_, index) => (
          <Skeleton key={index} className="h-11 w-full" />
        ))}
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <EmptyState
        title={emptyTitle}
        description={emptyDescription}
        action={emptyAction}
      />
    );
  }

  return (
    // Wide tables scroll inside their own container so the page body never
    // scrolls sideways on a phone.
    <div className="overflow-x-auto">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-sand-soft">
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className={cx(
                  "whitespace-nowrap px-4 py-3 text-[0.6875rem] font-semibold uppercase tracking-[0.14em] text-slate",
                  column.align === "right" ? "text-right" : "text-left",
                  column.secondary && "hidden lg:table-cell",
                )}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={cx(
                "border-b border-sand-soft/70 last:border-0",
                onRowClick && "cursor-pointer hover:bg-paper-sunk/60",
              )}
            >
              {columns.map((column) => (
                <td
                  key={column.key}
                  className={cx(
                    "px-4 py-3 align-middle text-ink",
                    column.align === "right" && "text-right tabular",
                    column.secondary && "hidden lg:table-cell",
                  )}
                >
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

/**
 * Pagination driven entirely by the server's response.
 *
 * The counts come from the API, never from a client-side slice — the admin can
 * have thousands of orders and the browser only ever holds one page of them.
 */
export function Pagination({
  page,
  meta,
  onPageChange,
  itemNoun,
}: {
  page: number;
  meta: Pick<PagedResponse<unknown>, "totalCount" | "totalPages" | "hasPrevious" | "hasNext" | "pageSize">;
  onPageChange: (page: number) => void;
  itemNoun: string;
}) {
  if (meta.totalCount === 0) return null;

  const first = (page - 1) * meta.pageSize + 1;
  const last = Math.min(page * meta.pageSize, meta.totalCount);

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-sand-soft px-4 py-3">
      <p className="text-[0.8125rem] text-slate tabular">
        {first}–{last} of {meta.totalCount} {itemNoun}
      </p>

      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          size="sm"
          disabled={!meta.hasPrevious}
          onClick={() => onPageChange(page - 1)}
        >
          Previous
        </Button>
        <span className="px-1 text-[0.8125rem] text-slate tabular">
          Page {page} of {Math.max(meta.totalPages, 1)}
        </span>
        <Button
          variant="secondary"
          size="sm"
          disabled={!meta.hasNext}
          onClick={() => onPageChange(page + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}
