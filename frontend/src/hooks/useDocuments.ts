import { useCallback, useEffect, useState } from "react";
import { getDocuments } from "../api/documentsApi";
import type { DocumentFilter, DocumentListItem } from "../types/document";

export function useDocuments() {
  const [documents, setDocuments] = useState<DocumentListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<DocumentFilter>({
    page: 1,
    pageSize: 10,
  });

  const [refreshToken, setRefreshToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    let cancelled = false;

    setLoading(true);
    setError(null);

    getDocuments(filter, controller.signal)
      .then((data) => {
        if (cancelled) return;
        setDocuments(data.items);
        setTotalCount(data.totalCount);
      })
      .catch((err) => {
        if (cancelled || controller.signal.aborted) return;
        if (err instanceof DOMException && err.name === "AbortError") return;
        setError(err instanceof Error ? err.message : "Unknown error");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [filter, refreshToken]);

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), []);

  const setPage = (page: number, pageSize: number) => {
    setFilter((prev) => ({ ...prev, page, pageSize }));
  };

  const setSearch = (search: string) => {
    setFilter((prev) => ({ ...prev, search: search || undefined, page: 1 }));
  };

  const setType = (type: string | undefined) => {
    setFilter((prev) => ({ ...prev, type, page: 1 }));
  };

  const setDateRange = (dateFrom: string | undefined, dateTo: string | undefined) => {
    setFilter((prev) => ({ ...prev, dateFrom, dateTo, page: 1 }));
  };

  const setSort = (sortBy: string | undefined, sortDir: string | undefined) => {
    setFilter((prev) => ({ ...prev, sortBy, sortDir }));
  };

  return {
    documents,
    totalCount,
    loading,
    error,
    filter,
    setPage,
    setSearch,
    setType,
    setDateRange,
    setSort,
    refresh,
  };
}
