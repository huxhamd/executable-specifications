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

function messageFrom(response: HttpErrorResponse): string {
  return typeof response.error === 'string' && response.error
    ? response.error
    : 'Could not add todo';
}
