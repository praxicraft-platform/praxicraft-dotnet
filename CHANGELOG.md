# Changelog

## 0.1.1

- ci: release via tags only (no push to main) (#2)
- docs: add CODE_OF_CONDUCT and update CONTRIBUTING (#1)
- ci: auto-bump releases with GitHub Release + package publish

## [0.1.0] — 2026-08-21

### Added

- Initial Assess Public API SDK.
- `Client` with Bearer API-key auth (`PRAXICRAFT_API_KEY` / `PRAXICRAFT_API_BASE_URL`).
- Automatic retries on `429` / `5xx` / transport errors (default 2), honouring `Retry-After`.
- Typed / mapped errors from `{ error: { code, message } }`.
- Resources: org, assessments, invites, results, webhooks, pipelines.
- Webhook signature helper for `X-Praxicraft-Signature`.
