import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { QuestionApiService } from './question-api.service';

describe('QuestionApiService', () => {
  let service: QuestionApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(QuestionApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the trimmed question to the stable endpoint', () => {
    service.ask('  When may police stop and frisk a person?  ').subscribe();
    const request = http.expectOne('/api/questions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ question: 'When may police stop and frisk a person?' });
    request.flush({ answer: 'Supported.', insufficientEvidence: false, sources: [] });
  });
});
