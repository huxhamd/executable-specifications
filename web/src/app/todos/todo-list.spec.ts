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

  async function type(fixture: ComponentFixture<TodoList>, title: string): Promise<void> {
    const input = query(fixture, 'new-todo-title') as HTMLInputElement;
    input.value = title;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function submit(fixture: ComponentFixture<TodoList>, title: string): Promise<void> {
    await type(fixture, title);
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

  it('shows no title warning initially', async () => {
    const fixture = await render();

    expect(query(fixture, 'title-too-long-warning')).toBeNull();
  });

  it('shows no title warning for a title of exactly 255 characters', async () => {
    const fixture = await render();

    await type(fixture, 'a'.repeat(255));

    expect(query(fixture, 'title-too-long-warning')).toBeNull();
  });

  it('warns while typing a title of 256 characters', async () => {
    const fixture = await render();

    await type(fixture, 'a'.repeat(256));

    expect(query(fixture, 'title-too-long-warning')?.textContent?.trim()).toBe('The title is too long');
  });

  it('counts UTF-16 code units, so a surrogate pair counts as two', async () => {
    const fixture = await render();

    await type(fixture, '😀'.repeat(127) + 'a');
    expect(query(fixture, 'title-too-long-warning')).toBeNull();

    await type(fixture, '😀'.repeat(128));
    expect(query(fixture, 'title-too-long-warning')).not.toBeNull();
  });

  for (const [description, title] of [
    ['leading', ' ' + 'a'.repeat(255)],
    ['trailing', 'a'.repeat(255) + ' '],
    ['surrounding', '\t ' + 'a'.repeat(255) + ' \n'],
  ] as const) {
    it(`ignores ${description} whitespace when checking the title length, as the API does`, async () => {
      const fixture = await render();

      await type(fixture, title);

      expect(query(fixture, 'title-too-long-warning')).toBeNull();
    });
  }

  it('counts whitespace inside the title', async () => {
    const fixture = await render();

    await type(fixture, 'a'.repeat(127) + ' '.repeat(2) + 'a'.repeat(127));

    expect(query(fixture, 'title-too-long-warning')).not.toBeNull();
  });

  it('warns when a title is too long even after trimming', async () => {
    const fixture = await render();

    await type(fixture, ' ' + 'a'.repeat(256) + ' ');

    expect(query(fixture, 'title-too-long-warning')).not.toBeNull();
  });

  it('submits the title as typed, leaving trimming to the server', async () => {
    const fixture = await render();

    await submit(fixture, ' Buy milk ');

    expect(service.added).toEqual([' Buy milk ']);
  });

  it('removes the title warning once the title is short enough again', async () => {
    const fixture = await render();
    await type(fixture, 'a'.repeat(256));

    await type(fixture, 'a'.repeat(255));

    expect(query(fixture, 'title-too-long-warning')).toBeNull();
  });

  it('removes the title warning when a successful add clears the title', async () => {
    const fixture = await render();

    await submit(fixture, 'a'.repeat(256));

    expect(query(fixture, 'title-too-long-warning')).toBeNull();
  });

  it('still lets a too long title be submitted so the server can reject it', async () => {
    rejectWith({ errors: { title: ['The title is too long'] } });
    const fixture = await render();
    const title = 'a'.repeat(256);

    await type(fixture, title);
    expect((query(fixture, 'add-todo') as HTMLButtonElement).disabled).toBe(false);

    await submit(fixture, title);
    expect(service.added).toEqual([title]);
  });

  it('shows only the error after a too long title is rejected', async () => {
    rejectWith({ errors: { title: ['The title is too long'] } });
    const fixture = await render();

    await submit(fixture, 'a'.repeat(256));

    expect(query(fixture, 'error-message')?.textContent?.trim()).toBe('The title is too long');
    expect(query(fixture, 'title-too-long-warning')).toBeNull();
  });

  it('replaces the error with the warning when the edited title is still too long', async () => {
    rejectWith({ errors: { title: ['The title is too long'] } });
    const fixture = await render();
    await submit(fixture, 'a'.repeat(257));

    await type(fixture, 'a'.repeat(256));

    expect(query(fixture, 'error-message')).toBeNull();
    expect(query(fixture, 'title-too-long-warning')).not.toBeNull();
  });

  it('clears the error and shows no warning once the edited title is short enough', async () => {
    rejectWith({ errors: { title: ['The title is too long'] } });
    const fixture = await render();
    await submit(fixture, 'a'.repeat(256));

    await type(fixture, 'a'.repeat(255));

    expect(query(fixture, 'error-message')).toBeNull();
    expect(query(fixture, 'title-too-long-warning')).toBeNull();
  });

  it('clears the error when a retry succeeds without editing the title', async () => {
    service.addResult = throwError(() => new HttpErrorResponse({ status: 500 }));
    const fixture = await render();
    await submit(fixture, 'Buy milk');

    service.addResult = of({ id: 1, title: 'Buy milk', completed: false });
    query(fixture, 'add-todo')!.click();
    await fixture.whenStable();

    expect(query(fixture, 'error-message')).toBeNull();
  });
});
