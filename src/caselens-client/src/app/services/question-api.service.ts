import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { QuestionRequest, QuestionResponse } from '../models/question.models';

@Injectable({ providedIn: 'root' })
export class QuestionApiService {
  private readonly http = inject(HttpClient);

  ask(question: string): Observable<QuestionResponse> {
    const request: QuestionRequest = { question: question.trim() };
    return this.http.post<QuestionResponse>('/api/questions', request);
  }
}
