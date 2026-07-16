# todo-app

Frontend for the todo list: lists/adds/completes todos via `todo-backend`, and shows a cached random image.

## Run

```bash
docker build -t todo-app:latest .
```

Needs `PORT`, `TODO_BACKEND_URL`, `IMAGE_SOURCE_URL`, `IMAGE_PATH`, `IMAGE_CACHE_MINUTES`.
