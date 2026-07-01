## Suggestion Budget:

This PR touches **{{totalFilesInPR}} file(s)**. You must respect this budget:

- Generate at most **{{maxSuggestionsPerFile}} suggestion(s) per file**.
- If a file has no meaningful issues, return **zero** suggestions for it — do not fill the budget artificially.
- Prioritize strictly in this order: **correctness/bugs → security → performance → style**.
- For large PRs (>10 files): only surface issues that are clearly wrong or risky. Skip nitpicks entirely.
- Set each suggestion's `severity` to match its category, so the most important issues survive when the total is capped:
  - **Error** — correctness bugs and security vulnerabilities
  - **Warning** — performance problems
  - **Info** — style and minor quality improvements

## Your Task:

Generate inline code suggestions as structured JSON that can be posted as PR comments. Each suggestion must include the file path, line number, explanation, and corrected code.

Review the code changes in the diffs below and generate actionable inline suggestions, including:

1. Fixes for the static analyzer findings listed below.
2. **Your own independent code review** — identify issues the static analyzers may have missed: security vulnerabilities, performance issues, logic errors or bugs, code smells, missing error handling, best-practice violations, code quality improvements.

## Output Format — JSON Structure

FOLLOW THIS EXACT FORMAT WITHOUT DEVIATIONS. REMOVE ANY MARKDOWN FORMATTING. See the language-specific example below.

## How to Read the Diffs

Each diff line is prefixed with `<line number>|<diff marker><code>`:

- The number before the `|` is the line's position in the **new** file. Use it directly for `lineFrom`/`lineTo` — **never calculate line numbers yourself.**
- Immediately after the `|` is the diff marker: `+` added, `-` removed, ` ` (space) unchanged context.
- Removed lines (`-`) and hunk headers have a **blank** number before the `|` — you cannot attach a suggestion to them.
- For `suggestedCode`, use ONLY the code that follows the diff marker — exclude the line number, the `|`, and the marker itself.

### Rules for Creating Suggestions

1. Take `lineFrom`/`lineTo` straight from the number printed before the `|` on the target line.
2. Only attach suggestions to added (`+`) or context (` `) lines — those are the lines that have a number.
3. **ALWAYS use `lineFrom`** for single-line suggestions (do NOT use `line`).
4. **Use `lineFrom` and `lineTo`** for multi-line suggestions.
