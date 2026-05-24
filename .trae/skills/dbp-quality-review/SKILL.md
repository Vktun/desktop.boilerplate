---
name: "dbp-quality-review"
description: "Use for quality, architecture, security, startup, configuration, permissions, audit, session, persistence, and deployment reviews in this Desktop Boilerplate WPF/Prism repository. Invoke when the user asks for analysis, review, hardening, TODO planning, platform-readiness assessment, or risk checks."
---

# DBP Quality Review Skill

## Purpose

Use this skill to review Desktop Boilerplate changes for platform readiness. The project is intended to grow from a WPF management shell into a reusable upper-computer/industrial desktop platform, so quality reviews should emphasize correctness, configuration safety, module boundaries, and maintainability.

## First Reads

Read these before forming findings:

- `.trae/project_rules.md`
- `.trae/rules/security-and-configuration.md`
- `docs/PROJECT_REVIEW_AND_TODO.md`
- `docs/LOCAL_CONFIGURATION.md`
- the changed files or files named by the user
- related tests under `test/`

## Review Priority

Lead with concrete risks, ordered by severity:

1. Security and secret handling.
   - Real credentials committed.
   - Default passwords reachable in runtime paths.
   - Sensitive values written to logs.

2. Startup and configuration correctness.
   - Database initialization not awaited before DB-backed UI or services.
   - Missing config produces unclear failures.
   - Unsafe fallback connection strings.

3. Identity, permissions, and audit.
   - Hard-coded operator identity.
   - Duplicate permission sources that can drift.
   - Missing denied-path tests for protected actions.

4. Module and layer boundaries.
   - Business modules directly depend on each other.
   - Host shell gains customer-specific business logic.
   - ViewModels talk directly to persistence details without a reason.

5. WPF reliability and performance.
   - UI-thread blocking work.
   - Heavy synchronous data load in constructors.
   - Missing virtualization for large lists.
   - Resource dictionaries or bindings that cause unnecessary churn.

6. Test coverage and verification.
   - No focused unit tests for changed behavior.
   - Integration-sensitive change without documented database requirements.
   - Build/test command not run or not feasible.

## Analysis Method

- Trace behavior from entry point to service to persistence or event sink.
- Compare the change against the closest existing pattern before suggesting a new pattern.
- Prefer targeted fixes over broad refactors.
- Call out pre-existing risks separately from regressions introduced by a change.
- If a finding is uncertain, name the evidence needed to validate it.

## Report Format

For code reviews, use:

```text
Findings
- Severity - file:line - issue and impact

Open Questions
- Question or assumption

Summary
- Brief context of reviewed area

Verification
- Commands run or not run
```

For project analysis, use:

```text
Current Shape
Key Strengths
Main Risks
Recommended Next Steps
Suggested Skills/Rules/Agents
```

## Verification Commands

Use focused commands where possible:

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
dotnet build desktop.boilerplate.slnx
git diff --check
```

If the environment lacks the required .NET SDK, SQL Server, or Windows desktop workload, state that clearly and still perform static review.

