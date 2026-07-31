# CaseLens Progress

## Current Status

- Current assignment: Assignment 4 — generate and persist embeddings.
- Source inspected: `rayheinzelman/CaseLens-Mini`, `dev` branch, commit
  `a422dcdb4df051d5da656ab5ee08a1e419412350`.
- Last known working state: Assignment 3 stores deterministic, page-aware
  chunks with nullable `real[]` embeddings.
- Assignment 4 implementation is complete in source. Runtime build, tests, and
  database backfill still need to be run on a machine with the .NET SDK, local
  PostgreSQL, test connection secret, and OpenAI API key.

## Completed

- Added `IEmbeddingGenerator` as the testable provider boundary.
- Added an OpenAI embeddings adapter using `text-embedding-3-small` and
  configurable vector dimensions.
- Kept the API key out of tracked configuration.
- Added a resumable backfill service that:
  - loads chunks in deterministic document/chunk order;
  - rejects already-stored vectors whose dimensions do not match configuration;
  - generates only null or empty vectors in batches of 64;
  - validates every generated vector before saving;
  - saves after each batch so a failed run can resume without duplicating work.
- Integrated embedding backfill after the existing `--ingest` flow. Unchanged
  PDFs remain skipped by ingestion, while their missing vectors are still
  populated.
- Added focused tests for:
  - OpenAI request configuration and response index ordering;
  - rejection of unexpected vector dimensions;
  - generation of only missing chunk vectors;
  - refusal to mix inconsistent stored vector dimensions.
- Did not add similarity ranking, retrieval, question answering, or chat
  completion.

## User Secrets Setup

Set the OpenAI API key in the API project's user secrets. Do not put the key in
`appsettings.json`.

```powershell
dotnet user-secrets set "OpenAI:Embeddings:ApiKey" "<your-api-key>" --project src/CaseLens.Api
```

The existing local database secrets remain:

```powershell
dotnet user-secrets set "ConnectionStrings:CaseLensDatabase" "<local-postgres-connection-string>" --project src/CaseLens.Api
dotnet user-secrets set "ConnectionStrings:CaseLensTestDatabase" "<local-test-postgres-connection-string>" --project tests/CaseLens.Api.Tests
```

## Commands Checked

- `git diff --check`
  - Result: passed.
- `python -m json.tool src/CaseLens.Api/appsettings.json`
  - Result: passed.
- `dotnet --version`
  - Result: blocked because `dotnet` is not installed in the current workspace.
- `dotnet build CaseLensMini.slnx --no-restore`
  - Result: not run because `dotnet` is unavailable.
- `dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --no-restore`
  - Result: not run because `dotnet` is unavailable.

## Local Verification and Backfill

Run these commands from the repository root after setting the user secrets:

```powershell
dotnet build CaseLensMini.slnx
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj
dotnet run --project src/CaseLens.Api -- --ingest
```

Then verify that every stored chunk has the configured 1,536 dimensions:

```sql
SELECT
    COUNT(*) AS total_chunks,
    COUNT(embedding) AS embedded_chunks,
    MIN(cardinality(embedding)) AS minimum_dimensions,
    MAX(cardinality(embedding)) AS maximum_dimensions
FROM document_chunks;
```

Success means `total_chunks` equals `embedded_chunks`, and both dimension values
equal `1536`.

## Decisions

- Decision: use OpenAI's `text-embedding-3-small` model with 1,536 dimensions.
  - Reason: it is sufficient for the three-opinion demo and stores directly in
    the existing PostgreSQL `real[]` column without pgvector.
- Decision: send raw HTTP requests inside the OpenAI adapter.
  - Reason: provider-specific request and response types remain private to one
    class, while the rest of the application depends only on
    `IEmbeddingGenerator`.
- Decision: keep embeddings nullable in the schema.
  - Reason: ingestion and external embedding calls cannot be one atomic
    operation; nullable vectors allow safe, resumable backfill after a partial
    failure.

## Blockers / Known Issues

- The current workspace has no .NET SDK, PostgreSQL connection secrets, or
  OpenAI API key, so compilation, test execution, and the real database backfill
  could not be performed here.
- If an existing non-empty vector has a dimension other than 1,536, backfill
  intentionally stops before calling OpenAI rather than mixing incompatible
  vectors.

## Next Exact Step

- On the development machine, set the OpenAI user secret, run the build and
  tests, execute the `--ingest` command, and run the SQL consistency query above.
- After all four checks pass, begin Assignment 5: in-process cosine similarity
  retrieval. Do not add chat completion yet.

## Do Not Change

- Three-opinion corpus
- Local PostgreSQL
- In-process cosine similarity
- C# backend and Angular UI
- No Docker, pgvector, Python, agents, auth, OCR, or cloud deployment

## Assignment 5 Patch

Assignment 5 adds:

- an EF Core query that loads embedded chunks with document metadata;
- in-process cosine-similarity ranking;
- request/response DTOs;
- a development-only `POST /api/development/retrieval` endpoint;
- unit tests for cosine math, validation, ordering, and three known questions;
- a real-corpus `--evaluate-retrieval` command.

No chat-completion model, pgvector, Docker, or new database schema is used.

Apply and verify locally:

```powershell
git apply --check CaseLens-Assignment-5.patch
git apply CaseLens-Assignment-5.patch
dotnet build CaseLensMini.slnx
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj
dotnet run --project src/CaseLens.Api -- --evaluate-retrieval
```

The evaluation succeeds only when the expected opinion appears in the top
three results for all three questions:

- stop and frisk / reasonable suspicion -> `392 U.S. 1 (1968)`;
- excessive force during arrest -> `490 U.S. 386 (1989)`;
- vehicle search incident to arrest -> `556 U. S. 332 (2009)`.

The evaluation process exits with code `1` if any case misses.

## Assignment 6 Patch

Assignment 6 adds the grounded question-answering vertical slice:

- stable `POST /api/questions` request and response DTOs;
- an `IAnswerGenerationService` provider boundary and OpenAI adapter;
- a prompt builder that exposes only retrieved evidence;
- application-controlled `C1` through `Cn` evidence identifiers;
- structured answer output with answer text, evidence IDs, and refusal state;
- application-side removal of unknown and duplicate citation IDs;
- mapping of validated IDs to chunk, document, citation, page, passage, and score;
- conservative refusal when retrieval is below threshold, the model refuses, or
  no valid citation remains;
- generic `502` handling for provider failures without returning provider bodies,
  API keys, or exception details;
- focused tests for supported answers, unsupported questions, invalid citations,
  and prompt constraints.

### Secrets

```powershell
dotnet user-secrets set "OpenAI:Answers:ApiKey" "<your-api-key>" --project src/CaseLens.Api
```

### Verification

```powershell
dotnet build CaseLensMini.slnx
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj
dotnet run --project src/CaseLens.Api
```

Then call `POST /api/questions` with:

- supported: `When may police stop and frisk a person based on reasonable suspicion?`
- unsupported: `How should I draft my apartment lease?`

The supported response must contain at least one source whose `evidenceId` maps
to a retrieved chunk. The unsupported response must set `insufficientEvidence`
to `true` and return no sources.

### Blocker

The isolated workspace could not resolve GitHub or run the repository locally,
so build, test, PostgreSQL, and live provider verification remain required on
the development machine. The similarity threshold is intentionally configurable
and should be tuned only from the Assignment 8 evaluation set.

### Next Exact Step

Apply this patch, run the checks and two endpoint probes above, then proceed to
Assignment 7's one-page Angular UI without changing the backend architecture.

## Assignment 7 Patch

Assignment 7 adds the minimal Angular demonstration page:

- a clear CaseLens header and research-only / no-legal-advice notice;
- the fixed list of three indexed opinions;
- an accessible question form with disabled, loading, and safe error states;
- distinct supported-answer and insufficient-evidence presentations;
- expandable validated source cards with evidence ID, opinion, citation, page,
  chunk metadata, similarity score, and passage;
- typed Angular models matching the existing `POST /api/questions` contract;
- a focused question API service and local development proxy;
- client tests for the supported, unsupported, transport-error, and request paths.

No backend architecture, database schema, upload flow, Docker, pgvector, or UI
component library was added.

### Verification

```powershell
cd src/caselens-client
npm ci
npm test -- --run
npm run build
npm start
```

With PostgreSQL and the API running, verify:

- supported: `When may police stop and frisk a person based on reasonable suspicion?`
- unsupported: `How should I draft my apartment lease?`

The supported path must show an answer and at least one expandable validated
source. The unsupported path must show the insufficient-evidence response and
no invented legal guidance.

### Blocker
