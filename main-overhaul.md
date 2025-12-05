# Program.cs Overhaul Plan (CliFx-first)

## 1. Goals
1.1 Replace manual `CommandLineParser` switch with CliFx application builder.
1.2 Provide clear command/subcommand structure with built-in help and concise banner.
1.3 Prompt for missing inputs at runtime (no pre-typed args required) using Spectre prompts.
1.4 Keep logging/UX consistent via `SpectreLogger`; maintain fail-fast exits.
1.5 Preserve existing services (metadata, mail.tm, CLI comparison) and resiliency pipeline.

## 2. Command Structure (CliFx)
2.1 Root command: shows summary/help and routes to subcommands.
2.2 Subcommands:
    2.2.1 `scrape`: options `--verbose`, `--output`, `--format`; prompt when absent; show summary.
    2.2.2 `mail`: options `--action`, `--email`; prompt when absent; uses `MailTmService` end-to-end.
    2.2.3 `metadata`: options `--title`, `--artist`, optional `--discogs-token`; prompt when absent; default env token.
    2.2.4 `cli compare`: parent `cli` with subcommand `compare` to run existing runners.
2.3 Global options: `--help`, `--version`; no custom parser needed.

## 3. Runtime Input & UX
3.1 Use Spectre prompts for any missing option (text prompt, selection for actions, file path prompt for output).
3.2 Show concise banner + command description before executing.
3.3 Output tables via Spectre for results (metadata, mail summary), minimal noise.
3.4 Exit codes: 0 success; 1 for validation/known issues; propagate failures for unexpected errors.

## 4. Wiring & Services
4.1 Instantiate services inside commands: `MusicMetadataService`, `MailTmService`, existing `Resilience` usage remains.
4.2 Pull Discogs token from `DISCOGS_USER_TOKEN` env; prompt if missing.
4.3 Keep retry policies untouched; no DI container required.

## 5. File Changes
5.1 Rewrite `Program.cs` to host CliFx app and command classes (nested or separate partials).
5.2 Remove reliance on `CliHelper`/`CommandLineParser` in entrypoint; keep file for reference until cleanup.
5.3 Reuse `CliComparison` helpers for `cli compare` command.
5.4 Ensure namespaces stay file-scoped and records remain in place.

## 6. Tests & Validation
6.1 Update/add CLI-level tests (happy-path and prompt-path) using existing test project.
6.2 Build and run tests after refactor.

## 7. Rollout Steps
7.1 Implement `Program.cs` refactor (CliFx, commands, prompts, help).
7.2 Adjust or retire `CliHelper` usage in main entrypoint.
7.3 Verify build + tests.
7.4 Final review for instruction compliance (logging, naming, no `var`, constants in SCREAMING_SNAKE_CASE).
