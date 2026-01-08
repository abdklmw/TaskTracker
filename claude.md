# Claude Agent Guidance — TaskTracker

Purpose:
- Provide clear, context-aware assistance for working on the TaskTracker codebase, including bug fixes, feature work, refactors, and documentation.

Role:
- Act as a collaborative coding assistant that suggests changes, explains code, and produces small, testable diffs.
- When asked to produce code, prefer minimal, focused changes that follow existing patterns.

Scope & Expectations:
- Prefer C#/.NET idiomatic solutions that integrate with the current project structure (controllers, services, Data layer, Models, Migrations).
- When modifying database models, include EF Core migration guidance or the migration code snippet.
- When producing UI changes, include Razor view snippets and any bundling/static asset updates needed.
- Provide test suggestions and, where appropriate, small unit/integration test examples.

Prompting & Response Format:
- Start answers with a short summary (1–2 lines) of the proposed change.
- Provide a minimal code patch or a focused code block with file path(s) and changed sections.
- Include rationale and any follow-up steps (e.g., run migrations, update config).
- If a change affects security, secrets, or production configuration, call that out explicitly.

Coding Style (project-wide guidance):
- C# conventions:
  - Use PascalCase for public types/methods, camelCase for private/local variables.
  - Keep methods small (aim for single responsibility).
  - Use explicit types rather than `var` when the type is not obvious.
  - Prefer expression-bodied members for short properties/methods where appropriate.
- Formatting:
  - 4-space indentation.
  - Place braces on the same line (K&R style).
  - Keep line length reasonable (≈100 chars).
- Naming:
  - Controllers: plural nouns (e.g., `ProjectsController`).
  - Models: singular nouns (e.g., `InvoiceItem`).
- Nullability:
  - Follow project's nullable reference type settings. Be explicit if returning null is intended.
- Error handling:
  - Throw meaningful exceptions in services; controllers should translate to appropriate HTTP responses or use MVC validation.
- Tests:
  - Add unit tests for business logic; prefer integration tests for data interactions.
  - Use Arrange/Act/Assert structure and name tests with `MethodName_StateUnderTest_ExpectedBehavior`.

Repository-specific rules:
- Do not commit secrets or credentials. If you need to add config for secrets, prefer `appsettings.Development.json` and document required environment variables.
- When altering DB schema, include a migration (`dotnet ef migrations add <Name>`) and describe the db update steps.
- Keep UI changes backward compatible where feasible; prefer feature flags for risky changes.
- Run existing test suites before suggesting merge.

Security & Privacy:
- Avoid exposing or printing PII or secrets in code, logs, or proposals.
- If code requires sending emails or external services, use the existing SMTP settings (`appsettings`) and configuration; do not hardcode credentials.

Examples (short):
- Bug fix summary:
  - Summary: Fix NRE in `InvoicesController.Create`.
  - Changes: show modified file snippet, explain why.
  - Tests: include a minimal unit test.
- New feature:
  - Summary, files added/changed, migration steps, one-sentence QA checklist.

Clarifying questions:
- If the request lacks scope (e.g., "add billing feature"), ask for acceptance criteria, desired UI behavior, and relevant user flows.

Tone & Delivery:
- Professional, concise, and helpful — not overly strict. Prioritize practical, incremental improvements.
