# Troubleshooting `IngressCertificateExpiredWarning` in Kubernetes

## Issue
Your Ingress resource is reporting `IngressCertificateExpiredWarning`, which may indicate an expired or misconfigured TLS certificate.

## Diagnosis

### 1. Verify the Certificate in Kubernetes
```sh
kubectl get certificate -n <namespace>
```
Check if the certificate is expired.

### 2. Inspect the Certificate Secret
```sh
kubectl describe secret <secret-name> -n <namespace>
```
Ensure it contains valid `tls.crt` and `tls.key` data.

### 3. Check Certificate Issuer
```sh
kubectl describe issuer <issuer-name> -n <namespace>
```
Look for errors in issuer status.

### 4. Check Cert-Manager Logs
If using cert-manager:
```sh
kubectl logs -l app=cert-manager -n cert-manager
```

## Fixing Common Issues

### Cert-Manager Fails to Renew Certificate
- Manually delete and recreate the certificate.
- Ensure DNS challenges are configured correctly.

### Self-Signed Certificate Issues
- Use `kubectl create secret tls` to create a new secret.
- Update the Ingress resource with the new secret.

### Certificate Secret is Missing
Recreate it:
```sh
kubectl create secret tls my-tls-secret --cert=path/to/tls.crt --key=path/to/tls.key -n <namespace>
```

## Preventative Measures
- Configure alerts for expiring certificates.
- Set up auto-renewal policies with cert-manager.
- Use `kubectl get certificates -A` regularly to monitor certificate health.
