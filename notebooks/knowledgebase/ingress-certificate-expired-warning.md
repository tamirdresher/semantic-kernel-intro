# Resolving `IngressCertificateExpiredWarning` in Kubernetes

## Issue
The `IngressCertificateExpiredWarning` alert indicates that a TLS certificate used by an Ingress resource is expired or nearing expiration. This can cause HTTPS traffic to fail.

## Causes
- Let’s Encrypt or another certificate issuer has not renewed the certificate.
- The Kubernetes Certificate Manager (`cert-manager`) is not functioning correctly.
- The certificate secret is misconfigured or missing.

## Solution

### 1. Check Certificate Status
Run:
```sh
kubectl get certificate -A
```
Look for expired certificates and note the associated `SecretName`.

### 2. Check Cert-Manager Logs (if using cert-manager)
```sh
kubectl logs -n cert-manager deploy/cert-manager
```
Look for renewal failures.

### 3. Manually Renew the Certificate (if needed)
Trigger a renewal:
```sh
kubectl delete certificate <CERT_NAME> -n <NAMESPACE>
```
Cert-manager should recreate it automatically.

### 4. Ensure DNS Configuration
If using Let’s Encrypt, check that your DNS records are correctly resolving to your Ingress controller.

### 5. Restart Ingress Controller
```sh
kubectl rollout restart deployment <INGRESS_CONTROLLER>
```

### 6. Validate New Certificate
```sh
kubectl get secret <SECRET_NAME> -o jsonpath='{.data.tls\.crt}' | base64 --decode | openssl x509 -text -noout
```
Ensure the expiration date is updated.

## Prevention
- Use cert-manager with auto-renewal.
- Set up monitoring for certificate expiration using Prometheus alerts or external tools like `cronjob` with `openssl` checks.
