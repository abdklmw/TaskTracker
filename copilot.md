# GitHub Copilot / IDE Assistant Guidance — TaskTracker

Purpose:
- Provide in-editor completions and suggestions that follow the project's existing structure and conventions to accelerate development.

How Copilot should behave:
- Suggest short, focused snippets that match surrounding code style.
- Prefer adapting to existing patterns in the repository (naming, layering).
- When providing a full method or class, include XML doc comments (brief) if appropriate.

When to suggest:
- Boilerplate code (DTOs, constructors, simple controller actions).
- Common refactors (extract method, simplify LINQ).
- Test skeletons and simple assertions.

When not to suggest:
- Large architectural rewrites without explicit request.
- Secrets, credentials, or production API keys.
- Unreviewed SQL or EF patterns that may corrupt data.

Coding Style (for Copilot to follow):
- Align with repository C# style:
  - PascalCase for types and methods; camelCase for locals.
  - 4 spaces indentation; same-line braces.
  - Use `async`/`await` for I/O-bound operations.
  - Favor `IEnumerable<T>` or `IReadOnlyList<T>` in public APIs where mutation isn't needed.
- Use dependency injection through constructors for services.
- Prefer `ILogger<T>` for logging; don't log sensitive data.

Commit & PR guidance:
- When generating commit message suggestions, follow: `scope: short description` (e.g., `invoices: fix null ref in Create`).
- Provide a one-line summary and a short body describing the why and what.

Examples of inline prompts to Copilot:
- "Create an async action that returns `IActionResult` and loads a project by id, returning 404 if not found."
- "Add a unit test for `InvoiceCalculator.CalculateTotal` covering tax calculation."

Quality control:
- Always run or suggest the test(s).
- Prefer minimal changes that are easy to review.

Integration tips:
- For larger changes, generate a small PR description and list files changed.
- If generating migrations, provide the EF Core migration command and suggest running `dotnet ef database update`.

Tone:
- Helpful and pragmatic; give short rationale with suggestions.
