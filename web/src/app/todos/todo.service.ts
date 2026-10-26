import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Todo {
  id: number;
  title: string;
  completed: boolean;
}

@Injectable({ providedIn: 'root' })
export class TodoService {
  private readonly http = inject(HttpClient);

  list(): Observable<Todo[]> {
    return this.http.get<Todo[]>('/api/todos');
  }

  add(title: string): Observable<Todo> {
    return this.http.post<Todo>('/api/todos', { title });
  }
}
