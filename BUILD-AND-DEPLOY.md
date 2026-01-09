# Build and Deploy Guide

Quick reference for building the Docker image and deploying to Kubernetes.

## Docker Build Commands

### Build the Journal Application Image

```bash
# Navigate to the project root (where journal.sln is located)
cd /opt/project/vieteam/journal

# Build the Docker image
docker build -f src/Journal/Dockerfile -t coolserver/journal:latest .

# Build with a specific version tag (recommended for production)
docker build -f src/Journal/Dockerfile -t coolserver/journal:v1.0.0 .

# Build and push to Docker Hub
docker build -f src/Journal/Dockerfile -t coolserver/journal:v1.0.0 .
docker push coolserver/journal:v1.0.0

# Build and push to private registry
docker build -f src/Journal/Dockerfile -t myregistry.com/journal:v1.0.0 .
docker push myregistry.com/journal:v1.0.0
```

### Multi-platform Build (Optional)

```bash
# Build for multiple architectures
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f src/Journal/Dockerfile \
  -t coolserver/journal:v1.0.0 \
  --push \
  .
```

---

## Update Image Tag in Helm Chart

After building, update the image tag in `values.yaml`:

```yaml
journal:
  image:
    repository: coolserver/journal
    tag: "v1.0.0"  # Change from 'latest' to your version
```

---

## Kubernetes Deployment

### First-Time Installation

```bash
# 1. Add Helm repositories
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo add opensearch https://opensearch-project.github.io/helm-charts/
helm repo update

# 2. Navigate to chart directory
cd /data_docker/otp-project/vieteam/journal/journal-helm

# 3. Download dependencies
helm dependency update

# 4. Install the chart
helm install journal . -n journal --create-namespace

# 5. Watch the deployment
kubectl get pods -n journal -w
```

### Upgrade Existing Deployment

```bash
# After building a new image
cd /data_docker/otp-project/vieteam/journal/journal-helm

# Update values.yaml with new image tag, then upgrade
helm upgrade journal . -n journal

# Or upgrade with command-line override
helm upgrade journal . -n journal \
  --set journal.image.tag=v1.0.1
```

### Force Restart After Image Update

```bash
# If the image tag is 'latest', Kubernetes won't pull new version
# Force a restart to pull the new image:
kubectl rollout restart deployment/journal -n journal

# Watch the rollout
kubectl rollout status deployment/journal -n journal
```

---

## Database Configuration

### Quick Configuration Reference

Database connections are configured in:
1. **`values.yaml`** - Passwords and basic settings
2. **`templates/configmap.yaml`** - Full connection strings

### Key Configuration Files

| File | Purpose |
|------|---------|
| `values.yaml` | Database passwords, image tags, resource limits |
| `templates/configmap.yaml` | Database connection strings (appsettings.Docker.json) |
| `templates/deployment.yaml` | Mounts the ConfigMap to `/app/appsettings.Docker.json` |
| `templates/secrets.yaml` | Stores database passwords (alternative to values.yaml) |

### Database Connection Locations

**SQL Server:**
- Password: `values.yaml` → `sqlserver.auth.saPassword` (line 61)
- Connection: `templates/configmap.yaml` → `JournalDb.Host`

**MongoDB:**
- Password: `values.yaml` → `mongodb.auth.rootPassword` (line 107)
- Connection: `templates/configmap.yaml` → `MongoDb.Host`

**Cassandra:**
- Config: `values.yaml` → `cassandra.cluster.*` (line 127)
- Connection: `templates/configmap.yaml` → `CassandraDb.ContactPoint`

**OpenSearch:**
- Password: `values.yaml` → `opensearch.extraEnvs[0].value` (line 145)
- Connection: `templates/configmap.yaml` → `OpenSearch.Host`

**PostgreSQL (deployed but not used by app):**
- Password: `values.yaml` → `postgresql.auth.postgresPassword` (line 88)

### Change Database Passwords

Edit `values.yaml`:
```yaml
sqlserver:
  auth:
    saPassword: "YourNewPassword123!"  # Line 61

mongodb:
  auth:
    rootPassword: "YourMongoPassword"  # Line 107

opensearch:
  extraEnvs:
    - name: OPENSEARCH_INITIAL_ADMIN_PASSWORD
      value: "YourOpenSearchPass123!"  # Line 145
```

Then upgrade:
```bash
helm upgrade journal . -n journal
kubectl rollout restart deployment/journal -n journal
```

### Change Database Hosts/Ports

Edit `templates/configmap.yaml` to modify connection strings:
```yaml
data:
  appsettings.Docker.json: |
    {
      "JournalDb": {
        "Host": "{{ include "journal.fullname" . }}-sqlserver",
        "Port": "1433",
        "Database": "ssto-database"
      }
    }
```

---

## Complete Build and Deploy Workflow

### Development Workflow

```bash
# 1. Make code changes
cd /opt/project/vieteam/journal

# 2. Build new Docker image
docker build -f src/Journal/Dockerfile -t coolserver/journal:dev-$(date +%Y%m%d) .

# 3. Push to registry
docker push coolserver/journal:dev-$(date +%Y%m%d)

# 4. Update Helm chart
cd /data_docker/otp-project/vieteam/journal/journal-helm
# Edit values.yaml and change image.tag to new tag

# 5. Upgrade deployment
helm upgrade journal . -n journal

# 6. Check status
kubectl get pods -n journal
kubectl logs -f deployment/journal -n journal
```

### Production Workflow

```bash
# 1. Tag release
cd /opt/project/vieteam/journal
VERSION=v1.0.0

# 2. Build production image
docker build -f src/Journal/Dockerfile -t coolserver/journal:${VERSION} .
docker tag coolserver/journal:${VERSION} coolserver/journal:latest
docker push coolserver/journal:${VERSION}
docker push coolserver/journal:latest

# 3. Update production values
cd /data_docker/otp-project/vieteam/journal/journal-helm
# Use production values file

# 4. Deploy to production
helm upgrade journal . -n journal-prod \
  -f values-production-example.yaml \
  --set journal.image.tag=${VERSION}

# 5. Verify deployment
kubectl get pods -n journal-prod
kubectl rollout status deployment/journal -n journal-prod
```

---

## Verify Deployment

### Check Application Status

```bash
# Check all pods
kubectl get pods -n journal

# Check journal app logs
kubectl logs -f deployment/journal -n journal

# Check database pods
kubectl logs journal-sqlserver-0 -n journal
kubectl logs journal-mongodb-0 -n journal
```

### Test Database Connectivity

```bash
# Port-forward to journal app
kubectl port-forward -n journal svc/journal 8080:80

# Access the app
curl http://localhost:8080/health

# Test SQL Server connection
kubectl exec -it journal-sqlserver-0 -n journal -- \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'SqlServer2022!' -Q "SELECT @@VERSION"
```

### Check Configuration

```bash
# View ConfigMap with database connections
kubectl get configmap journal-config -n journal -o yaml

# View the mounted configuration in the pod
kubectl exec -it deployment/journal -n journal -- cat /app/appsettings.Docker.json
```

---

## Troubleshooting

### Image Pull Issues

```bash
# Check image pull status
kubectl describe pod <pod-name> -n journal

# If using private registry, create image pull secret
kubectl create secret docker-registry regcred \
  --docker-server=myregistry.com \
  --docker-username=user \
  --docker-password=pass \
  -n journal

# Update values.yaml to use the secret
journal:
  imagePullSecrets:
    - name: regcred
```

### Database Connection Issues

```bash
# Check if databases are ready
kubectl get pods -n journal

# Test DNS resolution
kubectl run -it --rm debug --image=busybox -n journal -- \
  nslookup journal-sqlserver

# Check service endpoints
kubectl get endpoints -n journal
```

### Configuration Not Applied

```bash
# Check if ConfigMap updated
kubectl get configmap journal-config -n journal -o yaml

# Force pod restart to reload config
kubectl rollout restart deployment/journal -n journal

# Delete pod to force recreation
kubectl delete pod -l app.kubernetes.io/name=journal -n journal
```

---

## Quick Commands Reference

```bash
# Build image
docker build -f src/Journal/Dockerfile -t coolserver/journal:v1.0.0 .

# Install chart
helm install journal . -n journal --create-namespace

# Upgrade chart
helm upgrade journal . -n journal

# Rollback
helm rollback journal -n journal

# Uninstall
helm uninstall journal -n journal

# View logs
kubectl logs -f deployment/journal -n journal

# Port forward
kubectl port-forward -n journal svc/journal 8080:80

# Restart deployment
kubectl rollout restart deployment/journal -n journal
```

---

## File Locations Summary

| Purpose | File Path |
|---------|-----------|
| Dockerfile | `/opt/project/vieteam/journal/src/Journal/Dockerfile` |
| Helm Chart | `/data_docker/otp-project/vieteam/journal/journal-helm/` |
| Image Config | `journal-helm/values.yaml` (line 5-8) |
| DB Passwords | `journal-helm/values.yaml` (lines 61, 88, 107, 145) |
| DB Connections | `journal-helm/templates/configmap.yaml` |
| App Deployment | `journal-helm/templates/deployment.yaml` |

For detailed database configuration, see [DATABASE-CONFIGURATION.md](./DATABASE-CONFIGURATION.md)
