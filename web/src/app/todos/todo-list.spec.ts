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

  it('shows the message from a rejected add and keeps the entered title', async () => {
    service.addResult = throwError(
      () => new HttpErrorResponse({ status: 400, error: 'Title is required' }),
    );
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('Title is required');
    expect(titles(fixture)).toEqual([]);
    expect((query(fixture, 'new-todo-title') as HTMLInputElement).value).toBe('Buy milk');
  });

  it('shows a fallback message when a failed add has no message', async () => {
    service.addResult = throwError(() => new HttpErrorResponse({ status: 500 }));
    const fixture = await render();

    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('Could not add todo');
  });

  it('clears the error after a successful add', async () => {
    service.addResult = throwError(() => new HttpErrorResponse({ status: 500 }));
    const fixture = await render();
    await submit(fixture, 'Buy milk');

    service.addResult = of({ id: 1, title: 'Buy milk', completed: false });
    await submit(fixture, 'Buy milk');

    expect(query(fixture, 'error-message')).toBeNull();
  });
});
