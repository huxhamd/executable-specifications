import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Todo, TodoService } from './todo.service';

// Matches the API's limit, which applies to the trimmed title.
// String.length counts UTF-16 code units, as the API does.
const maxTitleLength = 255;

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
  // The error takes over after a rejection, so the user only ever sees one message.
  protected readonly showTitleTooLongWarning = computed(
    () => this.newTitle().trim().length > maxTitleLength && !this.error(),
  );

  constructor() {
    this.todoService.list().subscribe((todos) => this.todos.set(todos));
  }

  protected titleEdited(title: string): void {
    this.newTitle.set(title);
    this.error.set(null);
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
