# RenderVN AI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a public, demo-ready full-stack RenderVN AI prototype in eight weeks: 3D landing page, upload/draw input, AI rendering, projects, history, credit ledger and sandbox payment.

**Architecture:** A Next.js frontend talks to an ASP.NET Core Web API. The Core API owns users, projects, render jobs, credits and payments; a FastAPI service owns image preprocessing and the Replicate adapter. PostgreSQL stores transactional data and Cloudinary stores images. The product is a hackathon prototype, not a production CAD or 3D modeling platform.

**Tech Stack:** Next.js + TypeScript, Fabric.js, GSAP, Spline, ASP.NET Core 8 Web API, ASP.NET Identity, Entity Framework Core, PostgreSQL 16, Python 3.11, FastAPI, Pillow/OpenCV, Replicate, Cloudinary, VNPay sandbox, xUnit, pytest, Vitest and Playwright.

---

## Fixed decisions

- Primary image provider: Replicate.
- Primary image storage: Cloudinary.
- Primary payment integration: VNPay sandbox; local development uses a clearly labeled mock gateway.
- One render job creates two images and reserves 4 credits.
- Supported rooms: living room, bedroom and kitchen.
- Supported styles: Modern, Japandi, Indochine and Minimalist.
- Canvas is raster sketching only: brush, eraser, stroke width, undo, redo, clear and PNG export.
- If the schedule slips, remove payment and admin polish before removing the live render flow.

## Target repository layout

~~~text
D:\RenderVN AI\
  RenderVN.sln
  apps\web\
  services\core-api\
  services\core-api.Tests\
  services\ai-engine\
  infra\
  tests\
  docs\
  .env.example
  docker-compose.yml
~~~

## Daily vibe-coding protocol

For every checkbox below:

1. Ask the coding assistant for one small change only and include the exact file paths and acceptance criteria.
2. Review every changed line before accepting it.
3. Run the listed test or smoke check immediately.
4. Keep secrets in local environment files, never in prompts or source files.
5. Commit one coherent change at the end of the work session after running tests.

When a generated change fails, paste the exact error and the relevant file into the next prompt. Do not ask the assistant to rewrite the entire repository.

## Task 1: Bootstrap the repository and local infrastructure (Week 1, Days 1-2)

**Files:**
- Create: `D:\RenderVN AI\.gitignore`
- Create: `D:\RenderVN AI\.env.example`
- Create: `D:\RenderVN AI\docker-compose.yml`
- Create: `D:\RenderVN AI\apps\web\package.json`
- Create: `D:\RenderVN AI\services\core-api\RenderVN.CoreApi.csproj`
- Create: `D:\RenderVN AI\services\core-api.Tests\RenderVN.CoreApi.Tests.csproj`
- Create: `D:\RenderVN AI\services\ai-engine\pyproject.toml`
- Test: `D:\RenderVN AI\tests\smoke\README.md`

- [ ] **Step 1: Initialize version control and ignore generated files**

Run from `D:\RenderVN AI`:

~~~powershell
git init
dotnet new sln -n RenderVN
dotnet new webapi -n RenderVN.CoreApi -o services/core-api --framework net8.0
dotnet new xunit -n RenderVN.CoreApi.Tests -o services/core-api.Tests --framework net8.0
dotnet sln RenderVN.sln add services/core-api/RenderVN.CoreApi.csproj
dotnet sln RenderVN.sln add services/core-api.Tests/RenderVN.CoreApi.Tests.csproj
dotnet add services/core-api.Tests/RenderVN.CoreApi.Tests.csproj reference services/core-api/RenderVN.CoreApi.csproj
npx create-next-app@latest apps/web --ts --eslint --app --src-dir --use-npm --no-tailwind
python -m venv services/ai-engine/.venv
~~~

Create `services/ai-engine/pyproject.toml` with Python 3.11+, runtime dependencies `fastapi`, `uvicorn`, `pydantic-settings`, `httpx`, `pillow`, `opencv-python-headless`, `replicate` and `cloudinary`, plus test dependencies `pytest` and `pytest-asyncio`. Activate the virtual environment and run `pip install -e .` from `services/ai-engine`.

Add `.gitignore` entries for `node_modules`, `.next`, `bin`, `obj`, `.venv`, `.env`, `__pycache__`, test screenshots and local uploads. Expected result: `git status` shows source files but no secrets or build output.

- [ ] **Step 2: Add the local PostgreSQL service**

Create `docker-compose.yml` with this exact local-only service:

~~~yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: rendervn
      POSTGRES_USER: rendervn
      POSTGRES_PASSWORD: rendervn_local
    ports:
      - "5433:5432"
    volumes:
      - rendervn_postgres:/var/lib/postgresql/data
volumes:
  rendervn_postgres:
~~~

Run:

~~~powershell
docker compose up -d postgres
docker compose ps
~~~

Expected result: the PostgreSQL container is `running` and reachable at `localhost:5433`.

- [ ] **Step 3: Add environment keys without values**

Create `.env.example` containing:

~~~dotenv
DATABASE_CONNECTION=Host=localhost;Port=5433;Database=rendervn;Username=rendervn;Password=rendervn_local
CLOUDINARY_URL=cloudinary://key:secret@cloud
REPLICATE_API_TOKEN=replace-me
AI_CALLBACK_SECRET=replace-me
VNPAY_TMN_CODE=replace-me
VNPAY_HASH_SECRET=replace-me
VNPAY_RETURN_URL=http://localhost:3000/payment/return
CORE_API_BASE_URL=http://localhost:5080
~~~

Copy it to local-only `.env` files; do not commit those files.

- [ ] **Step 4: Add health endpoints before feature work**

Make ASP.NET return `{ "status": "ok" }` at `GET /health` and FastAPI return the same JSON at `GET /health`. Add `tests/smoke/README.md` with the exact checks:

~~~powershell
Invoke-RestMethod http://localhost:5080/health
Invoke-RestMethod http://localhost:8000/health
~~~

Expected result: both responses contain `status=ok`.

Install the initial frontend and test dependencies:

~~~powershell
cd apps/web
npm install fabric gsap @splinetool/react-spline
npm install -D vitest jsdom @testing-library/react @testing-library/jest-dom @playwright/test
cd ../..
~~~

- [ ] **Step 5: Commit the foundation**

~~~powershell
git add .
git commit -m "chore: bootstrap RenderVN AI workspace"
~~~

## Task 2: Build the visual shell and scroll-driven landing (Week 1, Days 3-5)

**Files:**
- Create: `apps/web/src/app/page.tsx`
- Create: `apps/web/src/app/globals.css`
- Create: `apps/web/src/components/landing/ScrollRoom.tsx`
- Create: `apps/web/src/components/landing/HeroCopy.tsx`
- Create: `apps/web/src/components/landing/SceneFallback.tsx`
- Create: `apps/web/src/components/landing/LandingSections.tsx`
- Test: `apps/web/src/components/landing/ScrollRoom.test.tsx`

- [ ] **Step 1: Define the visual tokens and page sections**

Use a warm architectural palette: charcoal ink, limestone background, terracotta accent and muted jade action color. Keep the font pairing expressive but readable, such as `DM Sans` for interface text and `Fraunces` for display headings. Define CSS variables in `globals.css`; do not use a purple-on-white default.

- [ ] **Step 2: Add the responsive landing structure**

`page.tsx` must render `HeroCopy`, `ScrollRoom` and `LandingSections`. The page must include a visible CTA linking to `/app`, a problem statement, a three-step process, style cards, sample results and a final CTA. Add a semantic `main` and `nav`.

- [ ] **Step 3: Add the Spline scene and fallback**

`ScrollRoom.tsx` loads the approved Spline scene only in the browser, pins it for five viewport heights and exposes scene states `sketch`, `build`, `scan`, `styles` and `reveal`. `SceneFallback.tsx` renders a static WebP when WebGL is unavailable, Spline fails, or `prefers-reduced-motion` is enabled.

- [ ] **Step 4: Connect scroll state to content**

Use GSAP ScrollTrigger for one pinned scene and keep no more than five state transitions. Test at 1440x900, 390x844 and with reduced motion. Expected result: the landing remains usable if the 3D scene is removed.

- [ ] **Step 5: Run the first visual smoke check**

~~~powershell
cd apps/web
npm run lint
npm run build
~~~

Expected result: no lint errors and a successful production build. Commit with `feat: add 3d landing shell`.

## Task 3: Implement Core API identity and domain schema (Week 2)

**Files:**
- Create: `services/core-api/Data/AppDbContext.cs`
- Create: `services/core-api/Domain/Project.cs`
- Create: `services/core-api/Domain/SourceImage.cs`
- Create: `services/core-api/Domain/RenderJob.cs`
- Create: `services/core-api/Domain/RenderResult.cs`
- Create: `services/core-api/Domain/CreditWallet.cs`
- Create: `services/core-api/Domain/CreditTransaction.cs`
- Create: `services/core-api/Domain/PaymentOrder.cs`
- Create: `services/core-api/Domain/StylePreset.cs`
- Create: `services/core-api/Domain/AuditEvent.cs`
- Create: `services/core-api/Features/Auth/AuthEndpoints.cs`
- Create: `services/core-api/Features/Projects/ProjectEndpoints.cs`
- Create: `services/core-api/Migrations/`
- Test: `services/core-api.Tests/Domain/CreditLedgerTests.cs`

- [ ] **Step 1: Add the domain entities and enums**

Use explicit enums for `RenderJobStatus` (`Queued`, `Processing`, `Succeeded`, `Failed`), `SourceType` (`Upload`, `Canvas`) and `CreditTransactionType` (`Grant`, `Reserve`, `Consume`, `Refund`, `Purchase`). Every entity has a `Guid Id`, UTC timestamps and an owner relation where applicable.

- [ ] **Step 2: Configure PostgreSQL and migrations**

Register `NpgsqlDbContext`, configure unique user email, indexes on `RenderJobs.UserId + CreatedAt` and `CreditTransactions.WalletId + CreatedAt`, then run:

~~~powershell
dotnet add services/core-api package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add services/core-api package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add services/core-api package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project services/core-api
dotnet ef database update --project services/core-api
~~~

Expected result: all tables are created in the local `rendervn` database.

- [ ] **Step 3: Implement registration and login**

Add `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` and `GET /api/me`. Registration creates an Identity user, a wallet and one immutable `Grant` transaction of 20 credits. Use an HttpOnly cookie for the session.

- [ ] **Step 4: Write the credit invariant tests first**

`CreditLedgerTests.cs` must prove that a successful reserve/consume changes 20 credits to 16, a refund restores 20, and the same idempotency key cannot reserve twice. Expected initial run: tests fail until the ledger service exists.

- [ ] **Step 5: Implement projects**

Add `POST /api/projects`, `GET /api/projects`, `GET /api/projects/{id}` and `DELETE /api/projects/{id}`. Enforce ownership in the query, not only in the controller. Commit with `feat: add identity projects and credit domain`.

## Task 4: Build the dashboard and drawing workspace (Week 3)

**Files:**
- Create: `apps/web/src/app/(auth)/login/page.tsx`
- Create: `apps/web/src/app/(auth)/register/page.tsx`
- Create: `apps/web/src/app/app/page.tsx`
- Create: `apps/web/src/components/workspace/SourceModePicker.tsx`
- Create: `apps/web/src/components/workspace/SketchCanvas.tsx`
- Create: `apps/web/src/components/workspace/RenderForm.tsx`
- Create: `apps/web/src/lib/api.ts`
- Create: `services/core-api/Features/Uploads/UploadEndpoints.cs`
- Create: `services/core-api/Infrastructure/IImageStore.cs`
- Create: `services/core-api/Infrastructure/CloudinaryImageStore.cs`
- Test: `apps/web/src/components/workspace/SketchCanvas.test.tsx`

- [ ] **Step 1: Add the typed API client**

Create one fetch wrapper that sends credentials, parses JSON errors, and exposes typed methods for auth, projects, upload and render. Do not call `fetch` directly from page components.

- [ ] **Step 2: Implement auth screens**

Build login and registration forms with client-side validation, server error display and redirect to `/app`. Add a loading state that prevents double submission.

- [ ] **Step 3: Implement the Fabric.js canvas**

`SketchCanvas.tsx` must expose `exportPng(): Promise<Blob>`, support brush, eraser, stroke width, undo, redo and clear, and resize to its container. Disable CAD-like features intentionally. The test must verify that clicking clear removes objects and export returns an image blob.

- [ ] **Step 4: Add the protected upload endpoint**

Add `POST /api/uploads` accepting one authenticated multipart image. Validate the MIME type and 10 MB limit, upload it through `IImageStore`, and return `{ url, width, height, sourceType }`. Store Cloudinary credentials only in server configuration.

- [ ] **Step 5: Add upload and canvas modes**

`SourceModePicker` toggles `upload` and `canvas`. `RenderForm` requires exactly one source, a room type, a style and a non-empty request. For canvas mode, call `exportPng()` before uploading.

- [ ] **Step 6: Add the dashboard list**

Show recent projects, current credit balance and a prominent “Tạo thiết kế” action. Empty state must include a sample project button. Commit with `feat: add authenticated drawing workspace`.

## Task 5: Build and verify the FastAPI AI adapter (Week 3-4)

**Files:**
- Create: `services/ai-engine/app/main.py`
- Create: `services/ai-engine/app/config.py`
- Create: `services/ai-engine/app/schemas.py`
- Create: `services/ai-engine/app/image_pipeline.py`
- Create: `services/ai-engine/app/replicate_client.py`
- Create: `services/ai-engine/app/callback_client.py`
- Create: `services/ai-engine/tests/test_prompt_builder.py`
- Create: `services/ai-engine/tests/test_image_validation.py`

- [ ] **Step 1: Add request schemas and health route**

Define `RenderRequest` with `job_id`, `source_url`, `source_type`, `room_type`, `style`, `user_prompt` and `callback_url`. Define `RenderAccepted` with `job_id` and `provider_id`.

- [ ] **Step 2: Implement deterministic prompt presets**

Create four style dictionaries with positive and negative prompt fragments. The prompt builder must produce the same string for the same input, and tests must verify that `indochine` includes wood, rattan and warm light while rejecting unsupported style names.

- [ ] **Step 3: Implement image validation and preprocessing**

Use Pillow to reject unsupported MIME types, files over 10 MB, images smaller than 512x512 and images with an extreme aspect ratio. Convert accepted inputs to RGB PNG and resize the long edge to 1024 pixels.

- [ ] **Step 4: Add the Replicate adapter**

Read `REPLICATE_API_TOKEN` only from environment variables. Submit the versioned SDXL ControlNet pipeline with Canny/Depth for photos and Scribble/MLSD for sketches. Set the provider webhook to `POST /v1/webhooks/replicate`. The webhook downloads successful outputs, uploads them to Cloudinary, then calls the Core API callback with `AI_CALLBACK_SECRET`.

- [ ] **Step 5: Add a provider mock for tests**

Implement `MockReplicateClient` that returns a fixture image URL without network access. Tests must pass with the mock and never require a real token. Commit with `feat: add fastapi image generation adapter`.

## Task 6: Connect render jobs, callbacks and credit ledger (Week 4)

**Files:**
- Create: `services/core-api/Features/Renders/RenderEndpoints.cs`
- Create: `services/core-api/Features/Renders/RenderOrchestrator.cs`
- Create: `services/core-api/Features/Renders/RenderCallbackEndpoint.cs`
- Create: `services/core-api/Features/Credits/CreditLedger.cs`
- Modify: `apps/web/src/lib/api.ts`
- Create: `apps/web/src/app/app/render/[id]/page.tsx`
- Test: `services/core-api.Tests/Features/RenderOrchestratorTests.cs`
- Test: `tests/e2e/render-flow.spec.ts`

- [ ] **Step 1: Implement create-render transaction**

Add `POST /api/render-jobs`. In one database transaction, validate project ownership, reserve 4 credits using an idempotency key, create a queued job and commit. Return `202 Accepted` with the job ID.

- [ ] **Step 2: Call FastAPI after the database transaction**

The orchestrator sends the job to `POST /v1/render`. If the call fails, mark the job failed and refund the reservation. Never hold a database transaction open while waiting on AI.

- [ ] **Step 3: Process signed callbacks**

Add `POST /api/render-jobs/{id}/callback`. Validate `AI_CALLBACK_SECRET`, reject callbacks for unknown jobs, and make repeated success/failure callbacks idempotent. A success stores two `RenderResult` rows and consumes the reservation; failure creates one `Refund` transaction.

- [ ] **Step 4: Add polling for the frontend**

Add `GET /api/render-jobs/{id}` returning status, progress label, error code and result URLs. The render page polls every 3 seconds while queued or processing and stops after success/failure.

- [ ] **Step 5: Add end-to-end flow with a mock AI**

`render-flow.spec.ts` registers a test user, creates a project, draws a short sketch, submits a render, waits for the mock callback, verifies two result cards and verifies the balance moved from 20 to 16. Expected result: the full path passes without Replicate credentials.

## Task 7: Results, history and payment sandbox (Week 5)

**Files:**
- Create: `apps/web/src/app/app/render/[id]/results/page.tsx`
- Create: `apps/web/src/app/app/history/page.tsx`
- Create: `apps/web/src/app/app/billing/page.tsx`
- Create: `services/core-api/Features/Payments/IPaymentGateway.cs`
- Create: `services/core-api/Features/Payments/MockPaymentGateway.cs`
- Create: `services/core-api/Features/Payments/VnPayGateway.cs`
- Create: `services/core-api/Features/Payments/PaymentEndpoints.cs`
- Test: `services/core-api.Tests/Features/PaymentSignatureTests.cs`

- [ ] **Step 1: Build result comparison**

Show source image beside two render results, style and prompt metadata, download buttons and a “Render lại” action that confirms the 4-credit cost before submitting.

- [ ] **Step 2: Build history**

Add `GET /api/projects/{id}/render-jobs` with pagination. The history screen must show queued, processing, succeeded and failed states and link to the result page.

- [ ] **Step 3: Define the payment interface**

Use this boundary so local development is deterministic:

~~~csharp
public interface IPaymentGateway
{
    Task<PaymentIntent> CreateAsync(Guid userId, int credits, CancellationToken ct);
    bool VerifyCallback(IReadOnlyDictionary<string, string> values);
}
~~~

Register `MockPaymentGateway` in Development and `VnPayGateway` in Staging. The mock page must say “Mô phỏng thanh toán” and never resemble a real bank confirmation.

- [ ] **Step 4: Implement VNPay signature verification**

Sort callback fields, build the exact query string required by VNPay, calculate HMAC with `VNPAY_HASH_SECRET`, reject mismatched signatures and make `PaymentOrder` completion idempotent. `PaymentSignatureTests.cs` must cover valid, invalid and repeated callbacks.

- [ ] **Step 5: Add billing UI**

Display 20 free credits, 100-credit and 500-credit packages, current balance, transaction history and clear sandbox status. Commit with `feat: add results history and sandbox billing`.

## Task 8: Quality, security and observability (Week 6)

**Files:**
- Create: `services/core-api/Infrastructure/RateLimitConfig.cs`
- Create: `services/core-api/Infrastructure/ErrorHandlingMiddleware.cs`
- Create: `services/ai-engine/app/observability.py`
- Create: `apps/web/src/components/ErrorState.tsx`
- Create: `apps/web/src/app/admin/page.tsx`
- Create: `services/core-api/Features/Admin/AdminEndpoints.cs`
- Create: `tests/ai-evaluation/scorecard.csv`
- Create: `tests/ai-evaluation/README.md`

- [ ] **Step 1: Add consistent error contracts**

Return `{ code, message, requestId }` from Core API errors. Map validation, insufficient credit, not found, provider timeout and unexpected errors to stable codes. Frontend displays Vietnamese user messages while logging the request ID.

- [ ] **Step 2: Add rate limits and upload controls**

Limit login attempts, uploads and render creation per user/IP. Validate MIME type server-side, enforce the 10 MB limit and reject executable or archive content.

- [ ] **Step 3: Add structured logs and Sentry**

Log job ID, user ID hash, provider ID, duration, status and error code. Never log prompt secrets, API tokens or payment signatures. Add Sentry to Next.js and ASP.NET; use the request ID to correlate errors.

- [ ] **Step 4: Build the AI scorecard**

Create a CSV with 20-30 rows containing source path, room type, expected style, layout score, style score, artifact score, latency and cost. Add a README that defines the 1-5 scoring rubric and records the final 70% pass-rate calculation.

- [ ] **Step 5: Add the minimal admin diagnostics page**

Create an `Admin` Identity role and protect `GET /api/admin/summary`. Return user count, render counts by status, average render duration and recent failure codes; never return prompt text or secrets. The `/admin` page displays these values and redirects non-admin users.

- [ ] **Step 6: Run the security smoke check**

Verify no secret appears in `git grep -n "replicate_api_token\\|cloudinary://\\|vnpay"` except `.env.example` placeholders. Verify a user cannot read another user's project by changing an ID in the URL.

## Task 9: Deploy staging and responsive polish (Week 7)

**Files:**
- Create: `infra/render.yaml`
- Create: `infra/vercel.env.example`
- Create: `docs/deployment.md`
- Modify: `apps/web/src/components/landing/SceneFallback.tsx`
- Modify: `apps/web/src/app/globals.css`
- Create: `tests/e2e/mobile.spec.ts`

- [ ] **Step 1: Deploy PostgreSQL, Core API and AI Engine**

Create the PostgreSQL project in Supabase. Create a Render blueprint with one ASP.NET service and one FastAPI service, both using the Supabase connection string. Configure health checks, environment variables and non-sleeping instances for demo day. Record service URLs in `docs/deployment.md` without recording secrets.

- [ ] **Step 2: Deploy Next.js**

Connect the repository to Vercel, set the API base URL and configure the `/api` rewrite to ASP.NET. Confirm registration and cookie login work on the public HTTPS domain.

- [ ] **Step 3: Configure Cloudinary and Replicate callbacks**

Use HTTPS callback URLs, test one successful job and one failed job, then verify the Core API balance and status transitions.

- [ ] **Step 4: Add mobile fallback and accessibility checks**

Run the mobile Playwright test at 390x844. Confirm canvas controls remain reachable, 3D is replaced by fallback when reduced motion is requested, images have alt text and all primary actions are keyboard reachable.

- [ ] **Step 5: Run Lighthouse and production build**

Run the production build and Lighthouse against the public URL. Fix broken navigation, layout overflow, failed image loads and console errors before adding visual polish.

## Task 10: Freeze the hackathon demo and pitch (Week 8)

**Files:**
- Create: `docs/demo-script.md`
- Create: `docs/pitch-deck-outline.md`
- Create: `docs/qa-checklist.md`
- Create: `tests/fixtures/demo-user.json`
- Create: `tests/fixtures/demo-images/`
- Create: `docs/roadmap-after-hackathon.md`

- [ ] **Step 1: Prepare the demo account**

Create one account with 20 credits, three projects, prepared source images and one completed render. Verify the account can be reset by a single admin script or SQL seed command.

- [ ] **Step 2: Record the fallback video**

Record a 3-5 minute video showing landing scroll, drawing, render progress, result comparison, credit deduction and history. Store the final file outside Git if it exceeds repository limits and link it from `docs/demo-script.md`.

- [ ] **Step 3: Write the pitch**

Use this order: Vietnamese interior sales problem, slow 3D workflow, RenderVN AI flow, 3D landing experience, live result, measured quality/cost, target users, business model, responsible limitations and next steps.

- [ ] **Step 4: Run the final QA checklist**

Check registration, drawing, upload, render, result, download, history, credit, payment sandbox, mobile layout, reduced motion, API failure fallback and demo video. Record pass/fail and the exact defect for every failed item.

- [ ] **Step 5: Freeze scope and tag the demo**

Stop feature work 48 hours before judging. Fix only P0 defects: broken login, broken render, incorrect credit, inaccessible result or public deployment failure. Tag the final commit:

~~~powershell
git add .
git commit -m "release: freeze RenderVN AI hackathon demo"
git tag hackathon-demo
~~~

## Weekly acceptance gates

- **End of Week 1:** public-quality landing shell and local services have health checks.
- **End of Week 2:** a user can register, log in and create a project.
- **End of Week 3:** a user can draw or upload an input and submit a mocked render.
- **End of Week 4:** a live Replicate render can return two images and update credits.
- **End of Week 5:** results, history and sandbox billing work end-to-end.
- **End of Week 6:** errors, rate limits, logs and AI scorecard are in place.
- **End of Week 7:** public staging works on desktop and mobile.
- **End of Week 8:** demo, video, pitch and QA checklist are frozen.

## Scope cuts if the schedule slips

1. Replace VNPay with the labeled mock gateway.
2. Generate one image per job instead of two.
3. Remove the admin screen and keep admin data accessible through logs/database.
4. Reduce landing 3D to three states: sketch, scan and reveal.
5. Keep live AI for one tested input path and use prepared examples for other room types.

Never cut the live drawing/upload flow, the render result screen, the credit invariant or the fallback demo video.
