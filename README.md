# CaseLens Mini — Command Reference

This README collects the command-line commands used throughout the CaseLens Mini project.

> Run commands from the repository root unless a section says otherwise.

---

## 1. Clone and Open the Repository

```powershell
git clone https://github.com/rayheinzelman/CaseLens-Mini.git
cd CaseLens-Mini
git checkout dev
```

Check the current branch:

```powershell
git branch --show-current
```

Get the latest changes from the remote `dev` branch:

```powershell
git pull origin dev
```

---

## 2. Confirm Required Tools

```powershell
git --version
dotnet --version
dotnet ef --version
psql --version
node --version
npm --version
gh --version
```

List installed .NET SDKs:

```powershell
dotnet --list-sdks
```

List installed .NET runtimes:

```powershell
dotnet --list-runtimes
```

---

## 3. Restore and Build the .NET Solution

Restore NuGet packages:

```powershell
dotnet restore CaseLensMini.slnx
```

Build the full solution:

```powershell
dotnet build CaseLensMini.slnx
```

Build without restoring packages again:

```powershell
dotnet build CaseLensMini.slnx --no-restore
```

Build in Release configuration:

```powershell
dotnet build CaseLensMini.slnx --configuration Release
```

Clean generated .NET build output:

```powershell
dotnet clean CaseLensMini.slnx
```

---

## 4. PostgreSQL Setup

Open PostgreSQL's command-line client:

```powershell
psql -U postgres
```

Connect directly to a database:

```powershell
psql -h localhost -p 5432 -U postgres -d caselens
```

Create the development database from `psql`:

```sql
CREATE DATABASE caselens;
```

Create the test database:

```sql
CREATE DATABASE caselens_test;
```

List databases:

```sql
\l
```

Connect to a database:

```sql
\c caselens
```

List tables:

```sql
\dt
```

Describe a table:

```sql
\d legal_documents
```

```sql
\d document_chunks
```

Exit `psql`:

```sql
\q
```

---

## 5. Configure .NET User Secrets

The project uses .NET user secrets so database passwords and OpenAI API keys are not committed to Git.

### Development database

```powershell
dotnet user-secrets set "ConnectionStrings:CaseLensDatabase" "Host=localhost;Port=5432;Database=caselens;Username=postgres;Password=<your-password>" --project src/CaseLens.Api
```

### Test database

```powershell
dotnet user-secrets set "ConnectionStrings:CaseLensTestDatabase" "Host=localhost;Port=5432;Database=caselens_test;Username=postgres;Password=<your-password>" --project tests/CaseLens.Api.Tests
```

### OpenAI embeddings API key

```powershell
dotnet user-secrets set "OpenAI:Embeddings:ApiKey" "<your-api-key>" --project src/CaseLens.Api
```

### OpenAI answer-generation API key

```powershell
dotnet user-secrets set "OpenAI:Answers:ApiKey" "<your-api-key>" --project src/CaseLens.Api
```

The same OpenAI API key may be used for both OpenAI settings.

### List configured secrets

```powershell
dotnet user-secrets list --project src/CaseLens.Api
```

```powershell
dotnet user-secrets list --project tests/CaseLens.Api.Tests
```

### Remove an individual secret

```powershell
dotnet user-secrets remove "OpenAI:Embeddings:ApiKey" --project src/CaseLens.Api
```

### Clear all secrets for a project

```powershell
dotnet user-secrets clear --project src/CaseLens.Api
```

---

## 6. Entity Framework Core Commands

Install the EF Core command-line tool if it is missing:

```powershell
dotnet tool install --global dotnet-ef
```

Update an existing global installation:

```powershell
dotnet tool update --global dotnet-ef
```

Confirm EF Core is available:

```powershell
dotnet ef --version
```

### Create a migration

```powershell
dotnet ef migrations add InitialCreate --project src/CaseLens.Api --startup-project src/CaseLens.Api
```

For a later migration, replace `MigrationName`:

```powershell
dotnet ef migrations add MigrationName --project src/CaseLens.Api --startup-project src/CaseLens.Api
```

### List migrations

```powershell
dotnet ef migrations list --project src/CaseLens.Api --startup-project src/CaseLens.Api
```

### Apply migrations

```powershell
dotnet ef database update --project src/CaseLens.Api --startup-project src/CaseLens.Api
```

### Remove the latest unapplied migration

```powershell
dotnet ef migrations remove --project src/CaseLens.Api --startup-project src/CaseLens.Api
```

### Generate a SQL migration script

```powershell
dotnet ef migrations script --project src/CaseLens.Api --startup-project src/CaseLens.Api
```

Generate an idempotent migration script:

```powershell
dotnet ef migrations script --idempotent --project src/CaseLens.Api --startup-project src/CaseLens.Api
```

---

## 7. Ingest PDFs and Generate Embeddings

Run the CaseLens ingestion process:

```powershell
dotnet run --project src/CaseLens.Api -- --ingest
```

The ingestion process:

1. Reads the fixed opinion PDFs.
2. Extracts page-aware text.
3. Creates deterministic chunks.
4. Saves documents and chunks to PostgreSQL.
5. Generates missing embeddings.
6. Skips unchanged documents and already-generated embeddings.

### Expected opinion directory

```text
src\CaseLens.Api\Data\Opinions
```

Expected PDF files:

```text
arizonavgant.pdf
grahamvconnor.pdf
terryvohio.pdf
```

---

## 8. Verify Stored Documents and Embeddings

Open the development database:

```powershell
psql -h localhost -p 5432 -U postgres -d caselens
```

Count imported documents:

```sql
SELECT COUNT(*) AS document_count
FROM legal_documents;
```

Count chunks:

```sql
SELECT COUNT(*) AS chunk_count
FROM document_chunks;
```

Inspect documents:

```sql
SELECT
    id,
    title,
    citation,
    source_file_name
FROM legal_documents
ORDER BY id;
```

Inspect chunk ordering:

```sql
SELECT
    legal_document_id,
    page_number,
    chunk_index,
    LEFT(text, 120) AS text_preview
FROM document_chunks
ORDER BY legal_document_id, chunk_index;
```

Verify that every chunk has an embedding:

```sql
SELECT
    COUNT(*) AS total_chunks,
    COUNT(embedding) AS embedded_chunks,
    MIN(cardinality(embedding)) AS minimum_dimensions,
    MAX(cardinality(embedding)) AS maximum_dimensions
FROM document_chunks;
```

Success means:

```text
total_chunks = embedded_chunks
minimum_dimensions = 1536
maximum_dimensions = 1536
```

Find chunks with missing embeddings:

```sql
SELECT
    id,
    legal_document_id,
    page_number,
    chunk_index
FROM document_chunks
WHERE embedding IS NULL
   OR cardinality(embedding) = 0;
```

Check all distinct embedding dimensions:

```sql
SELECT
    cardinality(embedding) AS dimensions,
    COUNT(*) AS chunk_count
FROM document_chunks
WHERE embedding IS NOT NULL
GROUP BY cardinality(embedding)
ORDER BY dimensions;
```

---

## 9. Run Backend Tests

Run the complete backend test project:

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj
```

Run without restoring:

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --no-restore
```

Run in Release configuration:

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --configuration Release
```

Show more detailed test output:

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --logger "console;verbosity=detailed"
```

### Run a test class or group by name

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --filter FullyQualifiedName~DocumentIngestionServiceTests
```

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --filter FullyQualifiedName~Embedding
```

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --filter FullyQualifiedName~Retrieval
```

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --filter FullyQualifiedName~Answer
```

Run one exact test by its fully qualified name:

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --filter "FullyQualifiedName=Namespace.TestClass.TestMethod"
```

List tests without running them:

```powershell
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj --list-tests
```

---

## 10. Evaluate Retrieval Quality

Run the real-corpus retrieval evaluation:

```powershell
dotnet run --project src/CaseLens.Api -- --evaluate-retrieval
```

The evaluation checks questions involving:

- Stop and frisk under `Terry v. Ohio`
- Excessive force under `Graham v. Connor`
- Vehicle searches incident to arrest under `Arizona v. Gant`

The command should exit with a nonzero status if an expected opinion does not appear in the required top results.

---

## 11. Run the ASP.NET Core API

Start the API:

```powershell
dotnet run --project src/CaseLens.Api
```

Run using the Development environment:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/CaseLens.Api
```

Run on a specified URL:

```powershell
dotnet run --project src/CaseLens.Api --urls "http://localhost:5000"
```

Watch for source changes and restart automatically:

```powershell
dotnet watch --project src/CaseLens.Api run
```

---

## 12. Call the Retrieval Development Endpoint

The development endpoint retrieves chunks without calling the answer-generation model.

### PowerShell

```powershell
$body = @{
    question = "What facts must support a stop and frisk?"
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:5000/api/development/retrieval" `
    -ContentType "application/json" `
    -Body $body
```

### curl

```powershell
curl.exe -X POST "http://localhost:5000/api/development/retrieval" `
  -H "Content-Type: application/json" `
  -d "{\"question\":\"What facts must support a stop and frisk?\"}"
```

---

## 13. Call the Question-Answering Endpoint

### Supported question with PowerShell

```powershell
$body = @{
    question = "When may police stop and frisk a person based on reasonable suspicion?"
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:5000/api/questions" `
    -ContentType "application/json" `
    -Body $body
```

### Unsupported question with PowerShell

```powershell
$body = @{
    question = "How should I draft my apartment lease?"
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:5000/api/questions" `
    -ContentType "application/json" `
    -Body $body
```

### Supported question with curl

```powershell
curl.exe -X POST "http://localhost:5000/api/questions" `
  -H "Content-Type: application/json" `
  -d "{\"question\":\"When may police stop and frisk a person based on reasonable suspicion?\"}"
```

### Unsupported question with curl

```powershell
curl.exe -X POST "http://localhost:5000/api/questions" `
  -H "Content-Type: application/json" `
  -d "{\"question\":\"How should I draft my apartment lease?\"}"
```

Use the actual API URL printed by `dotnet run` if it differs from `http://localhost:5000`.

---

## 14. Angular Setup

Move to the Angular project:

```powershell
cd src/caselens-client
```

Install dependencies exactly from `package-lock.json`:

```powershell
npm ci
```

Install dependencies while allowing `package.json` updates:

```powershell
npm install
```

Start the Angular development server:

```powershell
npm start
```

The application is normally available at:

```text
http://localhost:4200
```

Start Angular directly through the CLI:

```powershell
npx ng serve
```

Return to the repository root:

```powershell
cd ../..
```

---

## 15. Angular Tests and Build

From `src/caselens-client`:

```powershell
npm test -- --run
```

Run tests in watch mode:

```powershell
npm test
```

Build the Angular application:

```powershell
npm run build
```

Build using Angular CLI:

```powershell
npx ng build
```

Run Angular linting if a lint script has been configured:

```powershell
npm run lint
```

---

## 16. Full Local Verification Sequence

From the repository root:

```powershell
dotnet restore CaseLensMini.slnx
dotnet build CaseLensMini.slnx
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj
dotnet run --project src/CaseLens.Api -- --ingest
dotnet run --project src/CaseLens.Api -- --evaluate-retrieval
```

Then verify the Angular application:

```powershell
cd src/caselens-client
npm ci
npm test -- --run
npm run build
cd ../..
```

Start the backend:

```powershell
dotnet run --project src/CaseLens.Api
```

In a second terminal, start the frontend:

```powershell
cd src/caselens-client
npm start
```

---

## 17. Reset Local Demo Data

Connect to the development database:

```powershell
psql -h localhost -p 5432 -U postgres -d caselens
```

Delete all imported chunks and documents while keeping the schema:

```sql
TRUNCATE TABLE document_chunks, legal_documents RESTART IDENTITY CASCADE;
```

Exit `psql`:

```sql
\q
```

Reingest the fixed corpus:

```powershell
dotnet run --project src/CaseLens.Api -- --ingest
```

Use this carefully because regenerating embeddings makes new OpenAI API calls.

---

## 18. Git Status and Diff Checks

Check repository status:

```powershell
git status
```

Show unstaged changes:

```powershell
git diff
```

Show staged changes:

```powershell
git diff --cached
```

Check a diff for whitespace errors:

```powershell
git diff --check
```

Show recent commits:

```powershell
git log --oneline --decorate -10
```

Show the current commit:

```powershell
git rev-parse HEAD
```

Show the configured remotes:

```powershell
git remote -v
```

---

## 19. Commit and Push Changes

Stage all intended changes:

```powershell
git add .
```

Stage selected files:

```powershell
git add README.md PROGRESS.md
```

Commit:

```powershell
git commit -m "Complete CaseLens assignment"
```

Push the `dev` branch:

```powershell
git push origin dev
```

Set the upstream branch during the first push:

```powershell
git push --set-upstream origin dev
```

---

## 20. GitHub CLI Authentication

Authenticate GitHub CLI:

```powershell
gh auth login
```

Check authentication:

```powershell
gh auth status
```

Open the repository in a browser:

```powershell
gh repo view --web
```

View repository details:

```powershell
gh repo view
```

Create a pull request:

```powershell
gh pr create --base main --head dev --title "Complete CaseLens Mini" --body "Implements and verifies the remaining CaseLens Mini assignments."
```

List pull requests:

```powershell
gh pr list
```

Open the current pull request in a browser:

```powershell
gh pr view --web
```

---

## 21. Patch Commands

Check whether a patch applies cleanly:

```powershell
git apply --check CaseLens-Assignment-5.patch
```

Apply a patch:

```powershell
git apply CaseLens-Assignment-5.patch
```

Inspect a patch summary:

```powershell
git apply --stat CaseLens-Assignment-5.patch
```

Reverse an applied patch:

```powershell
git apply --reverse CaseLens-Assignment-5.patch
```

Replace the filename as needed for other assignment patches.

---

## 22. Rebase Commands and Recovery

Fetch the latest remote branches:

```powershell
git fetch origin
```

Rebase the current branch onto the remote `dev` branch:

```powershell
git rebase origin/dev
```

Continue after resolving conflicts:

```powershell
git add .
git rebase --continue
```

Abort the rebase:

```powershell
git rebase --abort
```

Skip the current rebase commit:

```powershell
git rebase --skip
```

Check rebase status:

```powershell
git status
```

### Error: previous rebase directory exists

If Git reports:

```text
fatal: previous rebase directory .git/rebase-apply still exists but mbox given
```

First try:

```powershell
git rebase --abort
```

If no valid rebase is active, remove the stale rebase state:

```powershell
Remove-Item -Recurse -Force .git\rebase-apply
```

Then check the repository:

```powershell
git status
```

Do not delete `.git\rebase-apply` while an active rebase contains work you still need.

---

## 23. Find Files and Inspect Build Output

Find the included opinion PDFs:

```powershell
Get-ChildItem -Path src\CaseLens.Api\Data\Opinions -Filter *.pdf
```

Find copied PDFs in build output:

```powershell
Get-ChildItem -Path src\CaseLens.Api\bin -Recurse -Filter *.pdf
```

Find all project files:

```powershell
Get-ChildItem -Recurse -Filter *.csproj
```

Find migration files:

```powershell
Get-ChildItem -Path src\CaseLens.Api -Recurse -Filter "*Migration*.cs"
```

Search source files for text:

```powershell
Get-ChildItem -Recurse -Include *.cs,*.ts,*.html,*.json |
    Select-String -Pattern "CaseLensDatabase"
```

---

## 24. Clean Build Artifacts

Remove backend `bin` and `obj` directories:

```powershell
Get-ChildItem -Path . -Recurse -Directory |
    Where-Object { $_.Name -in @("bin", "obj") } |
    Remove-Item -Recurse -Force
```

Restore and rebuild afterward:

```powershell
dotnet restore CaseLensMini.slnx
dotnet build CaseLensMini.slnx
```

Remove Angular dependencies and reinstall:

```powershell
cd src/caselens-client
Remove-Item -Recurse -Force node_modules
npm ci
cd ../..
```

---

## 25. Validate Configuration Files

Validate `appsettings.json` with Python:

```powershell
python -m json.tool src/CaseLens.Api/appsettings.json
```

Validate the development settings file:

```powershell
python -m json.tool src/CaseLens.Api/appsettings.Development.json
```

---

## 26. Search for Accidentally Committed Secrets

Check tracked files for common secret patterns:

```powershell
git grep -n -I -E "sk-[A-Za-z0-9_-]+|Password=|ApiKey"
```

Check the current diff:

```powershell
git diff
```

Check staged content:

```powershell
git diff --cached
```

If a real secret was committed, revoke and replace it. Removing it only from the latest file does not erase it from Git history.

---

## 27. Useful API and Process Checks

See whether the backend port is in use:

```powershell
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
```

See whether Angular's port is in use:

```powershell
Get-NetTCPConnection -LocalPort 4200 -ErrorAction SilentlyContinue
```

Find running .NET processes:

```powershell
Get-Process dotnet -ErrorAction SilentlyContinue
```

Stop all local .NET processes:

```powershell
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process
```

Find running Node processes:

```powershell
Get-Process node -ErrorAction SilentlyContinue
```

Stop all local Node processes:

```powershell
Get-Process node -ErrorAction SilentlyContinue | Stop-Process
```

Use the stop commands carefully if other .NET or Node applications are running.

---

## 28. Recommended Pre-Demo Command Sequence

### Terminal 1 — verification

```powershell
git status
git diff --check
dotnet build CaseLensMini.slnx --configuration Release
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj
dotnet run --project src/CaseLens.Api -- --evaluate-retrieval
```

### Terminal 2 — API

```powershell
dotnet run --project src/CaseLens.Api
```

### Terminal 3 — Angular

```powershell
cd src/caselens-client
npm ci
npm start
```

Then open:

```text
http://localhost:4200
```

Test a supported question:

```text
When may police stop and frisk a person based on reasonable suspicion?
```

Test an unsupported question:

```text
How should I draft my apartment lease?
```

---

## 29. Primary Project Commands at a Glance

```powershell
dotnet restore CaseLensMini.slnx
dotnet build CaseLensMini.slnx
dotnet test tests/CaseLens.Api.Tests/CaseLens.Api.Tests.csproj
dotnet ef database update --project src/CaseLens.Api --startup-project src/CaseLens.Api
dotnet run --project src/CaseLens.Api -- --ingest
dotnet run --project src/CaseLens.Api -- --evaluate-retrieval
dotnet run --project src/CaseLens.Api
```

```powershell
cd src/caselens-client
npm ci
npm test -- --run
npm run build
npm start
```

---

## Notes

- Keep PostgreSQL running while using the API, ingestion command, integration tests, or retrieval evaluation.
- Do not commit database passwords or OpenAI API keys.
- The project intentionally uses local PostgreSQL without Docker or `pgvector`.
- Retrieval is performed with in-process cosine similarity in C#.
- The fixed corpus consists of three public Supreme Court opinions.
- CaseLens does not support user-uploaded legal documents.

