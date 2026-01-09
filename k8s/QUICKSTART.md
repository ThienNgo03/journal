# Quick Start Guide

Get the Journal application running in Kubernetes in 5 minutes.

## Prerequisites

- Kubernetes cluster running
- `kubectl` configured
- `helm` 3.8+ installed

## Installation Steps

### 1. Add Helm Repositories

```bash
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo add opensearch https://opensearch-project.github.io/helm-charts/
helm repo update
```

### 2. Download Dependencies

```bash
cd journal-helm
helm dependency update
```

Expected output:
```
Hang tight while we grab the latest from your chart repositories...
...Successfully got an update from the "bitnami" chart repository
...Successfully got an update from the "opensearch" chart repository
Update Complete.
Saving 4 charts
Downloading postgresql from repo https://charts.bitnami.com/bitnami
Downloading mongodb from repo https://charts.bitnami.com/bitnami
Downloading cassandra from repo https://charts.bitnami.com/bitnami
Downloading opensearch from repo https://opensearch-project.github.io/helm-charts/
```

### 3. Update Configuration (Optional)

Edit `values.yaml` to change:
- Ingress hostname (line 20): `host: journal.yourdomain.com`
- Database passwords (recommended for production)

### 4. Install the Chart

```bash
# Create namespace and install
helm install journal . -n journal --create-namespace

# Watch the deployment
kubectl get pods -n journal -w
```

### 5. Access the Application

#### Option A: Via Ingress (Production)
1. Configure DNS: `journal.example.com` -> Your Ingress Controller IP
2. Access: `http://journal.example.com`

#### Option B: Port Forward (Development)
```bash
kubectl port-forward -n journal svc/journal 8080:80
# Access: http://localhost:8080
```

## Verify Installation

### Check All Pods are Running
```bash
kubectl get pods -n journal
```

Expected pods:
- `journal-xxxxx` (1 pod)
- `journal-postgresql-0` (1 pod)
- `journal-mongodb-0` (1 pod)
- `journal-cassandra-0` (1 pod)
- `journal-sqlserver-0` (1 pod)
- `journal-opensearch-0` (1 pod)

### Check Persistent Volumes
```bash
kubectl get pvc -n journal
```

All PVCs should be in `Bound` status with 5Gi size.

### Check Services
```bash
kubectl get svc -n journal
```

Expected services (all ClusterIP except journal):
- `journal` (ClusterIP)
- `journal-postgresql`
- `journal-mongodb`
- `journal-cassandra`
- `journal-sqlserver`
- `journal-opensearch`

## Database Connection Details

### From Inside the Cluster

**PostgreSQL:**
```
Host: journal-postgresql:5432
User: postgres
Password: postgrespw
Database: journaldb
```

**MongoDB:**
```
URI: mongodb://root:mongopw@journal-mongodb:27017/
Host: journal-mongodb:27017
User: root
```

**Cassandra:**
```
Host: journal-cassandra:9042
```

**SQL Server:**
```
Server: journal-sqlserver,1433
User: sa
Password: SqlServer2022!
```

**OpenSearch:**
```
URL: http://journal-opensearch:9200
User: admin
Password: MySecure@Pass123!
```

## Troubleshooting

### Pods in Pending State
```bash
# Check events
kubectl describe pod <pod-name> -n journal

# Common issue: No storage class available
kubectl get storageclass
```

### Database Connection Issues
```bash
# Check service endpoints
kubectl get endpoints -n journal

# Test database connectivity
kubectl run -it --rm --restart=Never test-postgres --image=postgres --namespace=journal -- psql -h journal-postgresql -U postgres
```

### View Logs
```bash
# Journal app logs
kubectl logs -n journal -l app.kubernetes.io/name=journal

# Database logs
kubectl logs -n journal journal-postgresql-0
kubectl logs -n journal journal-mongodb-0
kubectl logs -n journal journal-sqlserver-0
```

## Cleanup

```bash
# Uninstall the release
helm uninstall journal -n journal

# Delete PVCs (WARNING: This deletes all data!)
kubectl delete pvc --all -n journal

# Delete namespace
kubectl delete namespace journal
```

## Next Steps

1. Configure TLS/SSL for Ingress
2. Set up monitoring with Prometheus
3. Configure database backups
4. Review and adjust resource limits
5. Change default passwords for production

For detailed configuration options, see [README.md](./README.md).
