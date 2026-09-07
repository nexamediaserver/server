# Nexa

This repository contains the initial scaffold for the Nexa server and embedded web client.

## Architecture baseline

- Go server runtime
- SQLite-first storage model
- React + TypeScript frontend
- single binary deployment target
- mandatory setup gate before app use

## Repository layout

- `cmd/nexa/` — application entrypoint
- `internal/app/` — HTTP routing and app bootstrap shells
- `web/` — Vite React frontend
- `docs/` — architecture and implementation documentation

## Local development

### Go server

```bash
go run ./cmd/nexa
```

The server listens on http://localhost:8321 by default.

### Frontend

```bash
cd web
npm install
npm run dev
```

## Health checks

- `GET /healthz`
- `GET /api/v1/setup/status`
- `POST /api/v1/setup/start`
- `POST /api/v1/setup/complete`

## License

Nexa is licensed under the GNU Affero General Public License v3.0 or later
(AGPL-3.0-or-later). See [LICENSE](LICENSE) for the full license text.
