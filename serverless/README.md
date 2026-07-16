# serverless

Knative Serving demo: `manifests/hello.yaml` deploys `ghcr.io/knative/helloworld-go` as a Knative Service with two revisions ("World"/"Knative") split 50/50 traffic, tagged `current`/`candidate`.

## Run

Requires Knative Serving + Kourier installed on the cluster.

```bash
kubectl apply -f manifests/hello.yaml
```
