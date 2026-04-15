export interface DocumentListItem {
  id: number;
  type: string;
  date: string;
  firstName: string;
  lastName: string;
  city: string;
}

export interface DocumentItem {
  id: number;
  ordinal: number;
  product: string;
  quantity: number;
  price: number;
  taxRate: number;
}

export interface DocumentDetail {
  id: number;
  type: string;
  date: string;
  firstName: string;
  lastName: string;
  city: string;
  items: DocumentItem[];
}

export interface DocumentFilter {
  search?: string;
  type?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortDir?: string;
  page: number;
  pageSize: number;
}

export interface DocumentsResponse {
  items: DocumentListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ImportResponse {
  imported: number;
  updated: number;
  skippedItems: number;
}
