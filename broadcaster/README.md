# broadcaster

Subscribes to `todo_status` on NATS and forwards each todo create/update event as a message to a webhook (or just logs it if no webhook is set).

## Run

```bash
docker build -t broadcaster:latest .
```

Needs `NATS_URL`. Optional: `NATS_SUBJECT` (default `todo_status`), `GENERIC_WEBHOOK_URL`.
