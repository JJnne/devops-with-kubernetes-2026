# ping-pong-app

Counts pings in Postgres. `GET /` increments and returns the previous count, `GET /count` returns the current count, `GET /healthz` checks the DB connection.

## Run

```bash
docker build -t ping-pong-app:latest .
```

Needs `POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`. Listens on port 3000.

Runs as a Knative Service (`manifests/knative-service.yaml`), so it scales to zero when idle.
