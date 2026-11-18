## What this PR does

<!-- Short description and why. Link the issue: Closes #123 -->

## Context

<!-- Stops, Trips, Alerts, Ingestion, Game, Cluster, Frontend, Common, AppHost, CI -->

## Out of Scope

<!-- What this PR deliberately does not do. -->

## Checklist

- [ ] Build passes without warnings (`dotnet build gleis-grain.slnx`)
- [ ] Tests added or updated (`dotnet test --solution gleis-grain.slnx`)
- [ ] Persisted state or grain contracts changed? New fields use new `[Id]`s, `SchemaVersion` bumped if needed
- [ ] Endpoint or live event changed? the `.http` file in `src/Host/http` and the frontend are updated
- [ ] Frontend changed? `npm run type-check`, `npm run test` and `npm run build` pass in `src/Frontend`
- [ ] Package changed? `dotnet restore` run and the `packages.lock.json` files committed
- [ ] Commit messages follow [Conventional Commits](../CONTRIBUTING.md#commit-messages) with the context as scope
