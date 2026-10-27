import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { TodoList } from './todo-list';
import { Todo, TodoService } from './todo.service';

class FakeTodoService {
  todos: Todo[] = [];
  addResult: Observable<Todo> = of({ id: 1, title: '', completed: false });
  added: string[] = [];

  list(): Observable<Todo[]> {
    return of(this.todos);
  }

  add(title: string): Observable<Todo> {
    this.added.push(title);
    return this.addResult;
  }
}

describe('TodoList', () => {
  let service: FakeTodoService;

  beforeEach(async () => {
    service = new FakeTodoService();
    await TestBed.configureTestingModule({
      imports: [TodoList],
      providers: [{ provide: TodoService, useValue: service }],
    }).compileComponents();
  });

  async function render(): Promise<ComponentFixture<TodoList>> {
    const fixture = TestBed.createComponent(TodoList);
    await fixture.whenStable();
    return fixture;
  }

  function query(fixture: ComponentFixture<TodoList>, testId: string): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`);
  }

  function titles(fixture: ComponentFixture<TodoList>): string[] {
    return Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid="todo-title"]'),
    ).map((element) => element.textContent?.trim() ?? '');
  }

  async function submit(fixture: ComponentFixture<TodoList>, title: string): Promise<void> {
    const input = query(fixture, 'new-todo-title') as HTMLInputElement;
    input.value = title;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    query(fixture, 'add-todo')!.click();
    await fixture.whenStable();
  }

  it('shows the todos that already exist', async () => {
    service.todos = [
      { id: 1, title: 'Buy milk', completed: false },
      { id: 2, title: 'Walk dog', completed: true },
    ];

    const fixture = await render();

    expect(titles(fixture)).toEqual(['Buy milk', 'Walk dog']);
    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid="todo-item"]');
    expect(Array.from(items).map((item) => item.getAttribute('data-completed'))).toEqual([
      'false',
      'true',
    ]);
  });

  it('shows no error initially', async () => {
    const fixture = await render();

    expect(query(fixture, 'error-message')).toBeNull();
  });

  it('adds the entered title and shows the new todo', async () => {
    service.addResult = of({ id: 1, title: 'Buy milk', completed: false });
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect(service.added).toEqual(['Buy milk']);
    expect(titles(fixture)).toEqual(['Buy milk']);
  });

  it('clears the title input after adding', async () => {
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect((query(fixture, 'new-todo-title') as HTMLInputElement).value).toBe('');
  });

  it('submits a blank title so the server can decide whether to reject it', async () => {
    const fixture = await render();

    await submit(fixture, '');

    expect(service.added).toEqual(['']);
  });

  function rejectWith(error: unknown): void {
    service.addResult = throwError(() => new HttpErrorResponse({ status: 400, error }));
  }

  it('shows the validation message from a rejected add and keeps the entered title', async () => {
    rejectWith({ title: 'One or more validation errors occurred.', errors: { title: ['A title is required'] } });
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('A title is required');
    expect(titles(fixture)).toEqual([]);
    expect((query(fixture, 'new-todo-title') as HTMLInputElement).value).toBe('Buy milk');
  });

  it('shows the first validation message when there are several', async () => {
    rejectWith({ errors: { title: ['The title is too long', 'Second'], other: ['Third'] } });
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('The title is too long');
  });

  it('skips fields with no messages', async () => {
    rejectWith({ errors: { title: [], other: ['The title is too long'] } });
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('The title is too long');
  });

  it('shows a fallback message when a failed add has no message', async () => {
    service.addResult = throwError(() => new HttpErrorResponse({ status: 500 }));
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('Could not add todo');
  });

  for (const [description, error] of [
    ['a plain text body', 'Title is required'],
    ['no errors', { title: 'Bad Request' }],
    ['empty errors', { errors: {} }],
    ['errors that are not lists', { errors: { title: 'A title is required' } }],
    ['messages that are not text', { errors: { title: [42, ''] } }],
  ] as const) {
    it(`shows a fallback message when a rejection has ${description}`, async () => {
      rejectWith(error);
      const fixture = await render();

      await submit(fixture, 'Buy milk');

      expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('Could not add todo');
    });
  }

  it('clears the error after a successful add', async () => {
    service.addResult = throwError(() => new HttpErrorResponse({ status: 500 }));
    const fixture = await render();
    await submit(fixture, 'Buy milk');

    service.addResult = of({ id: 1, title: 'Buy milk', completed: false });
    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')).toBeNull();
  });
});
