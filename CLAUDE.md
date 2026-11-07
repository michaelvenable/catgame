When building gameplay, feel, audio, shell, or live features, load the `craft-game-floors` skill; if it isn't installed, ask the user whether to run `unity skill install <client>` (ask: never run it without a yes).

## Author / reviewer workflow

- **Author** = the main session. It implements the change, compiles it, and verifies it in the Editor (play mode, console, captures).
- **Reviewer** = the `reviewer` subagent (`.claude/agents/reviewer.md`), always launched fresh, never forked, so it doesn't inherit the author's reasoning.
- Before every commit, launch `reviewer` with only: the diff range (usually `git diff HEAD` plus untracked files) and one paragraph stating what the change should do. Don't pass the author's design rationale or known caveats.
- Fix every blocker and major finding, or reply with why it isn't a defect, verified against the code. After non-trivial fixes, run a fresh reviewer on the new diff.
- Commit only on `VERDICT: APPROVE`, and add a `Reviewed-by: reviewer agent` trailer to the commit message. Tell the user about any minor findings that were left open.
