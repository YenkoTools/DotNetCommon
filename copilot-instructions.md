# Copilot Instructions for DotNetCommon

## Repository purpose
- This repository contains a small collection of .NET class libraries.
- The libraries are intended to be reusable across multiple projects.
- Keep changes focused, predictable, and compatible with library consumers.

## Project conventions
- Target framework: `net10.0`.
- Nullable reference types are enabled.
- Implicit global usings are enabled.
- Language version is `latest`.
- Package versions are managed centrally through `Directory.Packages.props`.
- Prefer small, composable libraries over large framework-specific abstractions.

## Code style
- Follow the existing namespace and folder structure.
- Use file-scoped namespaces when adding new C# files.
- Prefer clear, minimal APIs with strong defaults.
- Preserve public API shape unless a change explicitly requires a breaking change.
- Keep naming consistent with the current CQRS, results, pipeline, metrics, and behavior patterns.
- Avoid introducing unnecessary dependencies.

## Library guidance
- Maintain the packages as reusable building blocks for other projects.
- Keep abstractions in the core library and optional integrations in separate packages.
- If adding new behaviors or services, keep optional dependencies isolated in their own project.
- Favor extension methods for dependency injection registration, matching the existing pattern.

## Validation and quality
- Prefer compile-time safety and simple control flow.
- Respect nullable annotations and do not suppress warnings without a clear reason.
- Add or update tests if the repository gains a test project in the future.
- If you change public APIs, check for downstream impact.

## Build and dependency rules
- Use centrally managed package versions only.
- Do not hardcode package versions inside individual project files unless there is a strong reason.
- Keep project files minimal and aligned with the existing SDK-style setup.

## Commit and attribution policy
- Do not add any agent attribution to commits.
- Do not add co-author trailers, bot signatures, or any AI/agent credit to commit messages.
- The human user must remain the sole owner of each commit.
- If preparing commit text or changelogs, keep them human-authored and neutral.

## When making changes
- Prefer the smallest correct change.
- Match existing formatting and conventions.
- Avoid unrelated refactors.
- Update documentation when behavior or public APIs change.
