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
  serverDate?: string | null;
}

export interface MeResponse {
  user: UserDto;
  serverDate?: string | null;
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

export interface BudgetItemDto {
  id: number;
  month: string;
  categoryId: number;
  categoryName: string;
  categoryParentName: string | null;
  color: string | null;
  amount: number;
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface BudgetCategoryRow {
  categoryId: number;
  name: string;
  parentName: string | null;
  color: string | null;
  planned: number;
  actual: number;
  diff: number;
}

export interface BudgetTotalsDto {
  planned: number;
  actual: number;
  diff: number;
}

export interface BudgetMonthResponse {
  month: string;
  items: BudgetItemDto[];
  categories: BudgetCategoryRow[];
  totals: BudgetTotalsDto;
}

export interface BudgetPayload {
  month: string;
  categoryId: number;
  amount: number;
  note?: string | null;
}

export interface SavingsContainerDto {
  id: number;
  name: string;
  color: string | null;
  sortOrder: number;
  createdAt: string;
}

export interface SavingsEntryDto {
  id: number;
  month: string;
  userId: number;
  userName: string;
  containerId: number;
  containerName: string;
  color: string | null;
  amount: number;
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SavingsMonthResponse {
  month: string;
  entries: SavingsEntryDto[];
  total: number;
}

export interface SavingsMonthAmount {
  month: string;
  amount: number;
}

export interface SavingsMonthDelta {
  month: string;
  delta: number;
}

export interface SavingsMemberTotal {
  userId: number;
  userName: string;
  amount: number;
}

export interface SavingsSummary {
  to: string;
  months: SavingsMonthAmount[];
  deltas: SavingsMonthDelta[];
  byMember: SavingsMemberTotal[];
  total: number;
}

export interface SavingsContainerPayload {
  name: string;
  color?: string | null;
  sortOrder?: number;
}

export interface SavingsEntryPayload {
  month: string;
  userId?: number;
  containerId: number;
  amount: number;
  note?: string | null;
}

export interface GoalDto {
  id: number;
  name: string;
  targetAmount: number;
  containerId: number;
  containerName: string;
  containerColor: string | null;
  dueDate: string | null; // 'yyyy-MM-dd'
  saved: number;
  monthlyRequired: number | null;
  achieved: boolean;
  createdAt: string;
}

export interface CreateGoalPayload {
  name: string;
  targetAmount: number;
  containerId: number;
  dueDate?: string | null; // 'yyyy-MM-dd'
}

export interface UpdateGoalPayload {
  name: string;
  targetAmount: number;
  containerId: number;
  dueDate: string | null; // 'yyyy-MM-dd'
}

export interface ApiErrorBody {
  code: string;
  message: string;
  errors?: Record<string, string | string[]>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
    public readonly errors?: Record<string, string | string[]>,
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
