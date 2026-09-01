import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CategoriesService, CategoryPayload } from '../../core/categories.service';
import { CategoryDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { CategoriesComponent } from './categories.component';

const tree: CategoryDto[] = [
  {
    id: 1,
    name: 'Зарплата',
    kind: 'INCOME',
    parentId: null,
    color: '#2E7D32',
    sortOrder: 1,
    isArchived: false,
    children: null,
  },
  {
    id: 2,
    name: 'Подработка',
    kind: 'INCOME',
    parentId: null,
    color: '#66BB6A',
    sortOrder: 2,
    isArchived: false,
    children: null,
  },
  {
    id: 3,
    name: 'Продукты',
    kind: 'EXPENSE',
    parentId: null,
    color: '#E5484D',
    sortOrder: 1,
    isArchived: false,
    children: [
      {
        id: 4,
        name: 'Супермаркет',
        kind: 'EXPENSE',
        parentId: 3,
        color: null,
        sortOrder: 1,
        isArchived: false,
        children: null,
      },
    ],
  },
];

describe('CategoriesComponent', () => {
  let createSpy: { calls: CategoryPayload[] };
  let deleteSpy: { calls: number[]; fail: boolean };

  beforeEach(async () => {
    createSpy = { calls: [] };
    deleteSpy = { calls: [], fail: false };
    await TestBed.configureTestingModule({
      imports: [CategoriesComponent],
      providers: [
        {
          provide: CategoriesService,
          useValue: {
            tree: () => of(tree),
            create: (p: CategoryPayload) => {
              createSpy.calls.push(p);
              return of({});
            },
            update: () => of({}),
            delete: (id: number) => {
              deleteSpy.calls.push(id);
              return deleteSpy.fail
                ? throwError(() => ({ status: 409, error: { code: 'Conflict', message: 'В категории есть операции — архивируйте её вместо удаления' } }))
                : of(undefined);
            },
          },
        },
        ToastService,
      ],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function comp() {
    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();
    return {
      fixture,
      component: fixture.componentInstance as unknown as {
        startCreate(kind: string): void;
        startCreateChild(cat: { id: number; name: string; kind: string; parentId: number | null; color: string | null; sortOrder: number; isArchived: boolean; level: number }): void;
        startEdit(cat: { id: number; name: string; kind: string; parentId: number | null; color: string | null; sortOrder: number; isArchived: boolean; level: number }): void;
        deleteCat(cat: { id: number; name: string; kind: string; parentId: number | null; color: string | null; sortOrder: number; isArchived: boolean; level: number }): void;
        draftName: { set(v: string): void };
        saveCreate(): void;
      },
    };
  }

  it('creates a group with the kind of the clicked section', () => {
    const { component: c } = comp();
    c.startCreate('INCOME');
    c.draftName.set('Премия');
    c.saveCreate();

    expect(createSpy.calls.length).toBe(1);
    expect(createSpy.calls[0].kind).toBe('INCOME');
    expect(createSpy.calls[0].parentId).toBeNull();
  });

  it('appends a new group after the last one of its section', () => {
    const { component: c } = comp();
    c.startCreate('EXPENSE');
    c.draftName.set('Поездки');
    c.saveCreate();

    expect(createSpy.calls[0].kind).toBe('EXPENSE');
    expect(createSpy.calls[0].sortOrder).toBe(2);
  });

  it('numbers new income groups after existing income groups', () => {
    const { component: c } = comp();
    c.startCreate('INCOME');
    c.draftName.set('Премия');
    c.saveCreate();

    expect(createSpy.calls[0].sortOrder).toBe(3);
  });

  it('inherits kind and parent for a new subcategory', () => {
    const { component: c } = comp();
    c.startCreateChild({
      id: 3,
      name: 'Продукты',
      kind: 'EXPENSE',
      parentId: null,
      color: '#E5484D',
      sortOrder: 1,
      isArchived: false,
      level: 0,
    });
    c.draftName.set('Базар');
    c.saveCreate();

    expect(createSpy.calls[0].kind).toBe('EXPENSE');
    expect(createSpy.calls[0].parentId).toBe(3);
    expect(createSpy.calls[0].sortOrder).toBe(2);
  });

  it('only offers the «+» subcategory button on group rows', () => {
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    const rows = Array.from(el.querySelectorAll<HTMLElement>('.divide-y > div'));
    expect(rows.length).toBe(4);
    rows.forEach((row, i) => {
      const plus = row.querySelector('button[title="Подкатегория"]');
      if (i < 2) {
        expect(plus, 'INCOME group rows must offer +').toBeTruthy();
      } else if (i === 2) {
        expect(plus, 'EXPENSE group row must offer +').toBeTruthy();
      } else {
        expect(plus, 'subcategory row must not offer +').toBeNull();
      }
    });
  });

  it('keeps the category and shows the server error when delete is rejected with 409', () => {
    deleteSpy.fail = true;
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const { fixture, component: c } = comp();
    const toasts = TestBed.inject(ToastService);
    const flat = {
      id: 4,
      name: 'Супермаркет',
      kind: 'EXPENSE',
      parentId: 3,
      color: null,
      sortOrder: 1,
      isArchived: false,
      level: 1,
    } as const;

    c.startEdit(flat);
    c.deleteCat(flat);
    fixture.detectChanges();

    expect(deleteSpy.calls).toEqual([4]);
    const tree = (fixture.componentInstance as unknown as { tree: () => CategoryDto[] }).tree();
    expect(tree.some((g) => g.id === 4 || g.children?.some((ch) => ch.id === 4))).toBe(true);
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Удалить');
    const list = toasts.toasts();
    expect(list.length).toBe(1);
    expect(list[0].kind).toBe('error');
    expect(list[0].message).toContain('архивируйте её вместо удаления');
  });

  it('removes the category and shows a success toast when delete succeeds', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const { fixture, component: c } = comp();
    const toasts = TestBed.inject(ToastService);

    c.deleteCat({ id: 4, name: 'Супермаркет', kind: 'EXPENSE', parentId: 3, color: null, sortOrder: 1, isArchived: false, level: 1 });
    fixture.detectChanges();

    expect(deleteSpy.calls).toEqual([4]);
    const list = toasts.toasts();
    expect(list.some((t) => t.kind === 'success' && t.message === 'Категория удалена')).toBe(true);
  });
});
