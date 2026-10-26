import { Component } from '@angular/core';
import { TodoList } from './todos/todo-list';

@Component({
  imports: [TodoList],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {}
