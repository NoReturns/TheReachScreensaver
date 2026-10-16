# The Reach Screensaver - agent guidance

This repository owns the Windows host, WebView2 integration, screensaver
modes, settings, reset handling, installation, and packaging. The sibling
Universe repository supplies the Reach web bundle and its voyage state.
Read the relevant code and BUILD_FROM_SOURCE.md before editing.

Keep changes focused and maintainable. Inspect Git status first, preserve
unrelated work, and make sensible decisions within the requested scope.
Use existing utilities where they fit; justify a different approach when it
improves the result. Investigate problems, challenge weak approaches, and
make routine decisions within the approved scope without unnecessary
permission requests.

Treat saved journeys, WebView2 profiles, settings, imagery caches, Windows
screensaver selection, and deployment backups as owner data. For changes to
contracts shared with Universe, check both repositories, including the
current checkpoint writer, migration behavior, and host reset confirmation
when relevant. If Universe is unavailable, report that limitation rather
than claiming compatibility. Test such changes with isolated fixtures; do
not use the owner's production journey as a test fixture.

Preserve fullscreen, Settings, and embedded preview behavior, including
native input exit and the isolation of preview from fullscreen progress.
For display changes, check one monitor and panoramic displays. Keep the
packaged web bundle usable offline and preserve required licenses and
attribution. Use packaging/INSTALL.md and packaging/ATTRIBUTION.md for
procedures and inventory.

Run focused validation suited to the change. Native build, reset/display
fixtures, and packaging script tests cover different risks; use only the
relevant ones unless broader validation is justified. Inspect Windows or
browser presentation when appearance is an acceptance criterion. Automated
checks do not establish owner visual acceptance.

Only the human owner may write to a remote. Agents must never run `git push`,
`git push --tags`, remote merges, `gh release create` or upload, cloud deployment
commands, or any other remote-write action, even if a later request casually
asks to publish. Agents may prepare local commits and checked commands for the
owner to execute manually. This rule has no implied exceptions.

Do not install, change Windows selection, remove backups, or reset owner data
without explicit authorization. A rehearsal or isolated fixture may proceed
within an authorized development task if it cannot affect owner state.

Report changed files, executed validation and results, remaining risks, and
unverified assumptions. Distinguish observed behavior from code findings
and inference. README.md, BUILD_FROM_SOURCE.md, and packaging/INSTALL.md
carry the detailed procedures.
