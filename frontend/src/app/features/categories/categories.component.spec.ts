import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
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

  beforeEach(async () => {
    createSpy = { calls: [] };
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
            delete: () => of(undefined),
          },
        },
        ToastService,
      ],
    });
  });

  function comp() {
    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();
    return fixture.componentInstance as unknown as {
      startCreate(kind: string): void;
      startCreateChild(cat: { id: number; name: string; kind: string; parentId: number | null; color: string | null; sortOrder: number; isArchived: boolean; level: number }): void;
      draftName: { set(v: string): void };
      saveCreate(): void;
    };
  }

  it('creates a group with the kind of the clicked section', () => {
    const c = comp();
    c.startCreate('INCOME');
    c.draftName.set('Премия');
    c.saveCreate();

    expect(createSpy.calls.length).toBe(1);
    expect(createSpy.calls[0].kind).toBe('INCOME');
    expect(createSpy.calls[0].parentId).toBeNull();
  });

  it('appends a new group after the last one of its section', () => {
    const c = comp();
    c.startCreate('EXPENSE');
    c.draftName.set('Поездки');
    c.saveCreate();

    expect(createSpy.calls[0].kind).toBe('EXPENSE');
    expect(createSpy.calls[0].sortOrder).toBe(2);
  });

  it('numbers new income groups after existing income groups', () => {
    const c = comp();
    c.startCreate('INCOME');
    c.draftName.set('Премия');
    c.saveCreate();

    expect(createSpy.calls[0].sortOrder).toBe(3);
  });

  it('inherits kind and parent for a new subcategory', () => {
    const c = comp();
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
});
