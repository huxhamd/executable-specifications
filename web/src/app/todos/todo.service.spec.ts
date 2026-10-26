import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { Todo, TodoService } from './todo.service';

describe('TodoService', () => {
  let service: TodoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TodoService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists todos from GET /api/todos', async () => {
    const todos: Todo[] = [{ id: 1, title: 'Buy milk', completed: false }];

    const result = firstValueFrom(service.list());
    const request = http.expectOne('/api/todos');
    expect(request.request.method).toBe('GET');
    request.flush(todos);

    expect(await result).toEqual(todos);
  });

  it('adds a todo by posting its title to /api/todos', async () => {
    const created: Todo = { id: 1, title: 'Buy milk', completed: false };

    const result = firstValueFrom(service.add('Buy milk'));
    const request = http.expectOne('/api/todos');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ title: 'Buy milk' });
    request.flush(created, { status: 201, statusText: 'Created' });

    expect(await result).toEqual(created);
  });
});
