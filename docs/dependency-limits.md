# Accepted Avalonia compatibility limit

Operator decision: 2026-10-12. Tracking record:
https://github.com/realgarit/realsnag-media-downloader/issues/25

Avalonia 11 is the supported application stack. PackageReference ranges have an
exclusive 12.0.0 upper bound, so a transitive dependency cannot silently select
Avalonia 12. Dependabot excludes the incompatible framework and theme versions;
compatible patches remain eligible for review.

The Avalonia 12 proposal fails with CS0122 because BindingPlugins is inaccessible. Keep the Semi theme below 12 along with the framework packages.

Reopen the record for a deliberate migration that aligns the framework, theme,
icons and test packages, replaces removed APIs, passes clean builds and tests,
and verifies representative native UI behavior. Remove the limits together in
that migration PR. This closes the queue item as an accepted limit rather than
claiming Avalonia 12 support.
