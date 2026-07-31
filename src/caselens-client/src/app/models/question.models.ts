export interface QuestionRequest {
  question: string;
}

export interface QuestionResponse {
  answer: string;
  insufficientEvidence: boolean;
  sources: QuestionSource[];
}

export interface QuestionSource {
  evidenceId: string;
  chunkId: number;
  documentTitle: string;
  citation: string;
  pageNumber: number;
  chunkIndex: number;
  passage: string;
  similarityScore: number;
}
