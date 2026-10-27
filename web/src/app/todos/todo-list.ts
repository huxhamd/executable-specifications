import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Todo, TodoService } from './todo.service';

@Component({
  imports: [],
  selector: 'app-todo-list',
  styleUrl: './todo-list.css',
  templateUrl: './todo-list.html',
})
export class TodoList {
  private readonly todoService = inject(TodoService);

  protected readonly todos = signal<Todo[]>([]);
  protected readonly newTitle = signal('');
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.todoService.list().subscribe((todos) => this.todos.set(todos));
  }

  protected add(event: Event): void {
    event.preventDefault();
    this.todoService.add(this.newTitle()).subscribe({
      next: (todo) => {
        this.todos.update((todos) => [...todos, todo]);
        this.newTitle.set('');
        this.error.set(null);
      },
      error: (response: HttpErrorResponse) => this.error.set(messageFrom(response)),
    });
  }
}

// Rejections are ValidationProblemDetails: { errors: { "<field>": ["<message>", ...] } }.
function messageFrom(response: HttpErrorResponse): string {
  const errors: unknown = response.error?.errors;
  const messages =
    errors && typeof errors === 'object'
      ? Object.values(errors).flatMap((field) => (Array.isArray(field) ? field : []))
      : [];
  return messages.find((message) => typeof message === 'string' && message) ?? 'Could not add todo';
}
