# log-output

Two containers sharing a volume: `writer` appends a timestamped random string to a log file every 5s, `reader` serves it over HTTP along with the ping-pong count.

## Run

```bash
docker build -t log-output-writer:latest writer/
docker build -t log-output-reader:latest reader/
```

Reader listens on port 3000, needs `MESSAGE` env var and the pingpong service reachable at `http://pingpong.exercises.svc.cluster.local`.

Deployed via the manifests in [devops-with-kubernetes-2026-config](https://github.com/JJnne/devops-with-kubernetes-2026-config)/log-output.
