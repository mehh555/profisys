import type { DocumentDetail, DocumentFilter, DocumentsResponse, ImportResponse } from "../types/document";

function buildFilterParams(filter: DocumentFilter, includePaging: boolean): URLSearchParams {
  const params = new URLSearchParams();
  if (filter.search) params.set("search", filter.search);
  if (filter.type) params.set("type", filter.type);
  if (filter.dateFrom) params.set("dateFrom", filter.dateFrom);
  if (filter.dateTo) params.set("dateTo", filter.dateTo);
  if (filter.sortBy) params.set("sortBy", filter.sortBy);
  if (filter.sortDir) params.set("sortDir", filter.sortDir);
  if (includePaging) {
    params.set("page", String(filter.page));
    params.set("pageSize", String(filter.pageSize));
  }
  return params;
}

async function parseErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const body = await response.json();
    return body?.detail || fallback;
  } catch {
    return fallback;
  }
}

export async function importDocuments(documentsFile: File, documentItemsFile: File): Promise<ImportResponse> {
  const formData = new FormData();
  formData.append("documents", documentsFile);
  formData.append("documentItems", documentItemsFile);

  const response = await fetch("/api/import", { method: "POST", body: formData });
  if (!response.ok) {
    throw new Error(await parseErrorMessage(response, "Import failed"));
  }
  return response.json();
}

export async function getDocuments(
  filter: DocumentFilter,
  signal?: AbortSignal
): Promise<DocumentsResponse> {
  const params = buildFilterParams(filter, true);
  const response = await fetch(`/api/documents?${params}`, { signal });
  if (!response.ok) {
    throw new Error(await parseErrorMessage(response, "Failed to fetch documents"));
  }
  return response.json();
}

export function exportDocumentsUrl(filter: DocumentFilter): string {
  const params = buildFilterParams(filter, false);
  return `/api/documents/export?${params}`;
}

export async function getDocumentById(id: number, signal?: AbortSignal): Promise<DocumentDetail> {
  const response = await fetch(`/api/documents/${id}`, { signal });
  if (!response.ok) {
    throw new Error(await parseErrorMessage(response, "Failed to fetch document"));
  }
  return response.json();
}
