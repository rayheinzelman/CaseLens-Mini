# <u>Personal Notes</u>

# Request Flow

## Embedding and Relevance Scoring
The CaseLens API generates an "embedding" for the inputted question. This is essentially a vector full of numbers that represent the semantic meaning of the question. Texts with similar meaning will generally produce vectors that point in similar directions when they are embedded. 

These vectors are a representation of the question in a high-dimensional space. The API then compares this embedding to the embeddings of the documents in the CaseLens database to find the most relevant documents that can help answer the question. The API returns a list of these relevant documents, along with their relevance scores, which indicate how closely they match the inputted question.

## Document Chunking
To get these document chunks and embed them, the CaseLens API first breaks down the documents into smaller chunks. This is done to ensure that the embeddings capture the context of the text more effectively. Each chunk is then embedded separately, allowing for a more granular comparison with the inputted question. The flow is essentially: 1. Obtain PDF, 2. Extract text by page, 3. Split text into DocumentChunkRecords, 4. Send each chunk's text to the OpenAI Embedding API, 5. Receive a vector representation for each chunk, 6. Store that vector in DocumentChunk.Embedding in PostreSQL. 

***See ChunkEmbeddingBackfillService.cs PopulateMissingAsync()***

At scale, we would not load all of these embeddings into application memory, instead we would use a vector database of PostreSQL's pgvector to store and perform similarity search. 

## Cosine Similarity
We first compare the question vector with every stored chunk vector using cosine similarity. A higher score means the question and chunk are more semantically related. We filter with two things in mind, 1. Keep only a limited number of the best results, i.e. the top 5, and 2. Keep only those results that are above a certain threshold of similarity.

*We use a threshold to determine which chunks are relevant "enough". This threshold is stored in appsettings.json, in the QuestionAnswering section*

***Cosine similarity measures the cosine of the angle between two vectors, which gives us a value between -1 and 1. A value closer to 1 indicates that the vectors are pointing in similar directions, meaning the texts are semantically similar.***

## Controlled Evidence IDs
Retrieved chunks are assigned temporary IDs, such as: C1, C2, C3, etc. These IDs are controlled by the C# application, not by the model. This is done in **QuestionAnswerService.cs in the AnswerAsync() method.** We then send this subset of chunks to the model along with the question. This reduces API tokens/cost. 

## Answer Model
The model receives the question and the relevant chunks, along with system instructions dictating how it should answer and what it should use. The model is instructed to use the provided chunks as evidence for its answer. It is also instructed to cite the evidence IDs in its response, which helps users trace back the information to the original documents. This way, we are constraining the model to only use the information we have deemed relevant.

The model returns a structured result, which is defined in **OpenAiAnswerGenerationService.cs's GenerateAsync() method.**

# Services
## Main Stages of Services
1. PDF Ingestion
2. Embedding Generation
3. Similarity Retrieval
4. Evidence constrained answer generation

##Ingestion Services
These services take the original PDF opinions and convert them into database records and text chunks, utilizing PdfPigTextExtractor. The actual chunking is done in **DocumentIngestionService.cs's IngestAsync() method**. 