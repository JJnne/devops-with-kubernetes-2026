# dummysite-controller

Custom controller for the `DummySite` CRD (`stable.dwk/v1`). Watches for `DummySite` resources and, for each one, fetches `spec.website_url` and creates a ConfigMap + nginx Deployment + Service that serve a copy of that page.

## Run

Needs to run in-cluster (uses `KubernetesClientConfiguration.InClusterConfig()`), with the RBAC/CRD from `manifests/`.

```bash
docker build -t dummysite-controller:latest .
kubectl apply -f manifests/
```

Example CR: `manifests/dummysite-sample.yaml`.
