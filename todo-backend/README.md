# todo-backend

REST API for todos, backed by Postgres. Publishes a NATS message on `todo_status` whenever a todo is created or its done-state changes.

## Run

```bash
docker build -t todo-backend:latest .
```

Needs `PORT`, `POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `NATS_URL`.

Endpoints: `GET /todos`, `POST /todos`, `PUT /todos/{id}`, `GET /healthz`, `POST /break` (flips health to unhealthy, for testing).
