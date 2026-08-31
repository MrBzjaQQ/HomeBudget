export type TxnKind = 'INCOME' | 'EXPENSE';

export interface UserDto {
  id: number;
  username: string;
  displayName: string;
  isActive: boolean;
  createdAt: string;
}

export interface LoginResponse {
  token: string;
  user: UserDto;
}

export interface CategoryDto {
  id: number;
  name: string;
  kind: TxnKind;
  parentId: number | null;
  color: string | null;
  sortOrder: number;
  isArchived: boolean;
  children: CategoryDto[] | null;
}

export interface TransactionDto {
  id: number;
  kind: TxnKind;
  amount: number;
  date: string;
  categoryId: number;
  categoryName: string;
  categoryParentName: string | null;
  color: string | null;
  userId: number;
  userDisplayName: string;
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface TotalsDto {
  income: number;
  expense: number;
  net: number;
}

export interface TransactionListResponse {
  items: TransactionDto[];
  total: number;
  page: number;
  pageSize: number;
  totals: TotalsDto;
}

export interface TransactionPayload {
  kind: TxnKind;
  amount: number;
  date: string;
  categoryId: number;
  userId?: number;
  note?: string | null;
}

export interface TransactionListParams {
  from?: string;
  to?: string;
  userId?: number;
  categoryId?: number[];
  kind?: TxnKind;
  q?: string;
  page?: number;
  pageSize?: number;
  sort?: 'date' | 'amount';
  dir?: 'asc' | 'desc';
}

export interface SummaryCategoryRow {
  categoryId: number;
  name: string;
  kind: TxnKind;
  parentId: number | null;
  parentName: string | null;
  color: string | null;
  income: number;
  expense: number;
}

export interface SummaryMonthRow {
  month: string;
  income: number;
  expense: number;
}

export interface ApiErrorBody {
  code: string;
  message: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
    public readonly errors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  static from(error: unknown): ApiError {
    if (error instanceof ApiError) {
      return error;
    }
    const body = (error as { error?: ApiErrorBody })?.error;
    const status = (error as { status?: number })?.status ?? 0;
    return new ApiError(
      status,
      body?.code ?? 'UNKNOWN',
      body?.message ?? 'Неизвестная ошибка',
      body?.errors,
    );
  }
}
