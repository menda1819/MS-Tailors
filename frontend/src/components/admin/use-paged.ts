"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { adminApi, describeError, toQuery } from "@/lib/admin-api";
import type { PagedResponse } from "@/types/admin";

const EMPTY_META = {
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
  hasPrevious: false,
  hasNext: false,
};

/**
 * Loads one server-rendered page of a list endpoint.
 *
 * Pagination, filtering and sorting all happen on the server; this only ever
 * holds the current page, so a list of ten thousand orders costs the same as a
 * list of ten.
 */
export function usePaged<T>(
  path: string,
  params: Record<string, string | number | boolean | undefined | null>,
) {
  const [items, setItems] = useState<T[]>([]);
  const [meta, setMeta] = useState(EMPTY_META);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Serialised so the effect compares by value, not by object identity — an
  // inline params object would otherwise refetch on every render.
  const query = toQuery(params);
  const requestId = useRef(0);

  const load = useCallback(async () => {
    const id = ++requestId.current;

    setLoading(true);
    setError(null);

    try {
      const response = await adminApi.get<PagedResponse<T>>(`${path}${query}`);

      // A slower earlier request must not overwrite a newer one's results.
      if (id !== requestId.current) return;

      setItems(response.items);
      setMeta({
        page: response.page,
        pageSize: response.pageSize,
        totalCount: response.totalCount,
        totalPages: response.totalPages,
        hasPrevious: response.hasPrevious,
        hasNext: response.hasNext,
      });
    } catch (caught) {
      if (id !== requestId.current) return;
      setError(describeError(caught));
      setItems([]);
      setMeta(EMPTY_META);
    } finally {
      if (id === requestId.current) setLoading(false);
    }
  }, [path, query]);

  useEffect(() => {
    // Fetching on mount is exactly the "subscribe to an external system" case
    // effects exist for. The lint rule cannot tell that apart from a derived
    // value that should have been computed during render.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  return { items, meta, loading, error, reload: load };
}

/** Loads a single resource, with the same loading and error shape. */
export function useResource<T>(path: string | null) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!path) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      setData(await adminApi.get<T>(path));
    } catch (caught) {
      setError(describeError(caught));
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [path]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  return { data, loading, error, reload: load, setData };
}
