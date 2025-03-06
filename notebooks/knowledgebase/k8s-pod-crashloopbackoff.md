# Resolving `CrashLoopBackOff` in Kubernetes Pods

## Issue
A pod enters a `CrashLoopBackOff` state, meaning it keeps restarting and failing.

## Common Causes
- Application crashes due to configuration issues.
- Missing environment variables or misconfigured secrets.
- Resource limits are too low.
- Readiness probes fail.

## Steps to Debug

### 1. Check Pod Logs
```sh
kubectl logs <pod-name> -n <namespace>
```
Identify errors before the crash.

### 2. Describe the Pod
```sh
kubectl describe pod <pod-name> -n <namespace>
```
Look for status messages and event logs.

### 3. Check Resource Limits
If the pod is being killed for exceeding resource limits:
```sh
kubectl get pod <pod-name> -o jsonpath='{.spec.containers[*].resources}'
```
Increase limits if necessary.

### 4. Verify Readiness and Liveness Probes
Check probe configurations in the pod spec:
```yaml
livenessProbe:
  httpGet:
    path: /healthz
    port: 8080
  initialDelaySeconds: 3
  periodSeconds: 10
```
If the probe is causing restarts, disable it temporarily.

### 5. Run the Pod Manually
If the issue persists, run the pod manually:
```sh
kubectl run debug-pod --image=<your-image> --rm -it -- bash
```
Manually execute commands inside the container.

## Resolution
- Fix configuration issues.
- Increase memory/CPU limits if the container is running out of resources.
- Update readiness probes if they are too strict.

## Prevention
- Use structured logging for debugging.
- Implement liveness probes correctly.
- Monitor pod restart counts with Prometheus or Kubernetes events.
