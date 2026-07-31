import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { QuestionResponse } from './models/question.models';
import { QuestionApiService } from './services/question-api.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './case-lens-page.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly questionApi = inject(QuestionApiService);

  protected readonly indexedOpinions = [
    'Terry v. Ohio — 392 U.S. 1 (1968)',
    'Graham v. Connor — 490 U.S. 386 (1989)',
    'Arizona v. Gant — 556 U.S. 332 (2009)',
  ];

  protected question = '';
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly response = signal<QuestionResponse | null>(null);

  protected ask(): void {
    if (this.loading() || !this.question.trim()) return;
    this.loading.set(true);
    this.errorMessage.set('');
    this.response.set(null);

    this.questionApi.ask(this.question).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => this.response.set(response),
      error: error => this.errorMessage.set(this.describeError(error)),
    });
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'CaseLens could not reach the local API. Confirm the API is running and try again.';
    }
    return 'CaseLens could not process the question. Please try again.';
  }
}
