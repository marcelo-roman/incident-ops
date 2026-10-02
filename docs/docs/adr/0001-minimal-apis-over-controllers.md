---
status: accepted
date: 2026-09-08
deciders: Marcelo Roman
---

# Minimal APIs over MVC controllers

## Context and problem statement

The Incidents API (`services/api`) exposes 13 endpoints over one aggregate (`Incident`) plus read models for services, on-call and metrics. The conventions require endpoints to only bind input, call the application layer and map the result. Which ASP.NET Core programming model keeps endpoints thin and makes that rule easy to enforce in review?

## Decision drivers

- Endpoints must not accumulate domain logic.
- Low startup time and memory on Container Apps consumption (scale from zero).
- OpenAPI generation for the public Swagger page.
- Familiar to .NET engineers joining the team.

## Considered options

1. Minimal APIs with route groups and endpoint filters
2. MVC controllers with `[ApiController]`
3. FastEndpoints (third-party)

## Decision outcome

Chosen option: **Minimal APIs**, grouped per resource (`/api/incidents`, `/api/services`, `/api/oncall`, `/api/metrics`), one static mapping class per group, validation as endpoint filters, errors mapped to RFC 7807 through `IProblemDetailsService`.

### Consequences

- Good: each endpoint is a few lines; any `if` about the domain in a mapping file is a visible review finding.
- Good: smaller startup path, measurable on cold starts (no MVC model binding and filter pipeline).
- Good: built-in OpenAPI support in .NET 8 with `WithOpenApi()`.
- Bad: no conventions for free (action filters, model state). We replace them explicitly with endpoint filters, which is more code to read once.
- Bad: engineers used to controllers need a short ramp-up; covered in the API README.

## Pros and cons of the options

### MVC controllers

- Good: widely known; attribute routing and filters out of the box.
- Bad: controllers grow into service classes; base-class conveniences make logic in actions cheap to add.
- Bad: heavier startup.

### FastEndpoints

- Good: REPR pattern enforces one class per endpoint.
- Bad: third-party dependency on the critical path for a 13-endpoint API; one more thing to upgrade with every .NET release.
