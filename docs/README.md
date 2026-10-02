# Documentation site

Documentation site for Incident Ops: architecture diagrams, bounded contexts, operations model, alerting, runbooks, engineering standards, leadership templates and architecture decision records. Published at <https://incidents-docs.marceloroman.com.br>.

## Contents

1. [Structure](#structure)
2. [Run locally](#run-locally)
3. [Writing pages](#writing-pages)
4. [Publishing](#publishing)
5. [License](#license)

## Structure

| Folder | Content |
|---|---|
| `docs/architecture` | C4 context and containers, context map, incident lifecycle, SLA timers and escalation, real-time, deployment and delivery, observability, code structure |
| `docs/operations` | support model, severity and SLA, alerting, incident response, postmortems, runbooks with KQL, KTLO metrics, SLOs |
| `docs/engineering` | Definition of Ready/Done, release readiness, code review, branching, quality gates, testing strategy, ADO hygiene |
| `docs/leadership` | stakeholder updates, KTLO vs roadmap, risk and dependency log, retrospective actions |
| `docs/adr` | architecture decision records (MADR) |

The interface contract the modules implement lives in [contracts/contracts.md](../contracts/contracts.md); pages link to it instead of copying it.

## Run locally

From this folder:

```bash
python3 -m venv .venv
.venv/bin/pip install -r requirements.txt
.venv/bin/mkdocs serve
```

Open http://127.0.0.1:8000. `mkdocs build --strict` is the same check CI runs.

## Writing pages

- Diagrams are Mermaid in fenced blocks (` ```mermaid `), rendered client-side by Material for MkDocs.
- Links between pages are relative paths to `.md` files; strict mode fails the build on broken links and anchors.
- Links to code use `https://github.com/marcelo-roman/incident-ops/tree/main/<path>` for folders and `/blob/main/<path>` for files.
- New pages are added to `nav` in `mkdocs.yml`.
- Admonitions (`!!! note`) and content tabs (`=== "Tab"`) are enabled.

## Publishing

[`.github/workflows/docs.yml`](../.github/workflows/docs.yml) runs on pull requests and pushes that change `docs/**` or the workflow itself. It builds with `--strict` on every run; on `main` it uploads the site and deploys it with `actions/deploy-pages` to GitHub Pages. The site is served at https://incidents-docs.marceloroman.com.br, a custom domain set in the repository's Pages settings and backed by a DNS-only Cloudflare CNAME to `marcelo-roman.github.io`; `site_url` in `mkdocs.yml` matches it.

## License

[MIT](../LICENSE)
