import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { QuestionApiService } from './services/question-api.service';

describe('App', () => {
  const questionApi = { ask: vi.fn() };
  beforeEach(async () => {
    questionApi.ask.mockReset();
    await TestBed.configureTestingModule({ imports: [App], providers: [provideHttpClient(), { provide: QuestionApiService, useValue: questionApi }] }).compileComponents();
  });
  it('renders the fixed opinion collection', () => { const f=TestBed.createComponent(App); f.detectChanges(); const t=f.nativeElement.textContent as string; expect(t).toContain('Terry v. Ohio'); expect(t).toContain('Graham v. Connor'); expect(t).toContain('Arizona v. Gant'); });
  it('renders a supported answer and source', () => { questionApi.ask.mockReturnValue(of({answer:'Supported.',insufficientEvidence:false,sources:[{evidenceId:'C1',chunkId:12,documentTitle:'Terry v. Ohio',citation:'392 U.S. 1 (1968)',pageNumber:9,chunkIndex:4,passage:'Specific and articulable facts.',similarityScore:.87}]})); const f=TestBed.createComponent(App); const a=f.componentInstance as any; a.question='When may police stop and frisk?'; a.ask(); f.detectChanges(); expect(f.nativeElement.textContent).toContain('Evidence-supported response'); expect(f.nativeElement.textContent).toContain('87% match'); });
  it('renders an insufficient-evidence refusal', () => { questionApi.ask.mockReturnValue(of({answer:'Not enough evidence.',insufficientEvidence:true,sources:[]})); const f=TestBed.createComponent(App); const a=f.componentInstance as any; a.question='How should I draft a lease?'; a.ask(); f.detectChanges(); expect(f.nativeElement.textContent).toContain('Insufficient evidence'); });
  it('does not leak provider error details', () => { questionApi.ask.mockReturnValue(throwError(() => new Error('provider secret'))); const f=TestBed.createComponent(App); const a=f.componentInstance as any; a.question='Question'; a.ask(); f.detectChanges(); const t=f.nativeElement.textContent as string; expect(t).toContain('could not process'); expect(t).not.toContain('provider secret'); });
});
