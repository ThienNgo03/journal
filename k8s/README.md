# Journal Application Helm Chart

A production-ready Helm chart for deploying the Journal application with multiple database backends on Kubernetes.

## Overview

This Helm chart deploys:
- **Journal Application**: ASP.NET Core application exposed via Ingress
- **PostgreSQL**: Bitnami chart v18.2.0
- **MongoDB**: Bitnami chart v18.1.20 (v6.0)
- **Cassandra**: Bitnami chart v12.3.11
- **SQL Server 2022**: Custom deployment
- **OpenSearch**: Official OpenSearch chart v2.23.1

All databases are configured with 5GB persistent storage and ClusterIP services (internal only).

## Prerequisites

- Kubernetes 1.19+
- Helm 3.8.0+
- PV provisioner support in the underlying infrastructure (for persistent volumes)
- Ingress controller installed (if using Ingress)

## Installation

### Step 1: Add Required Helm Repositories

```bash
# Add Bitnami repository for PostgreSQL, MongoDB, and Cassandra
helm repo add bitnami https://charts.bitnami.com/bitnami

# Add OpenSearch repository
helm repo add opensearch https://opensearch-project.github.io/helm-charts/

# Update repositories
helm repo update
```

### Step 2: Update Chart Dependencies

```bash
cd journal-helm
helm dependency update
```

This will download all the dependency charts (PostgreSQL, MongoDB, Cassandra, OpenSearch) into the `charts/` directory.

### Step 3: Customize Values

Edit `values.yaml` to customize your deployment:

1. **Update Ingress hostname**:
```yaml
journal:
  ingress:
    hosts:
      - host: journal.yourdomain.com  # Change this to your domain
```

2. **Update database passwords** (recommended for production):
```yaml
sqlserver:
  auth:
    saPassword: "YourStrongPassword123!"

postgresql:
  auth:
    postgresPassword: "YourPostgresPassword"

mongodb:
  auth:
    rootPassword: "YourMongoPassword"
```

3. **Adjust resource limits** based on your cluster capacity.

### Step 4: Install the Chart

```bash
# Install with default values
helm install journal . -n journal --create-namespace

# Or install with custom values
helm install journal . -n journal --create-namespace -f custom-values.yaml
```

## Configuration

### Key Configuration Parameters

#### Journal Application

| Parameter | Description | Default |
|-----------|-------------|---------|
| `journal.replicaCount` | Number of journal replicas | `1` |
| `journal.image.repository` | Journal image repository | `coolserver/journal` |
| `journal.image.tag` | Journal image tag | `latest` |
| `journal.ingress.enabled` | Enable ingress | `true` |
| `journal.ingress.className` | Ingress class name | `nginx` |
| `journal.ingress.hosts[0].host` | Ingress hostname | `journal.example.com` |

#### PostgreSQL (Bitnami)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `postgresql.enabled` | Enable PostgreSQL | `true` |
| `postgresql.auth.postgresPassword` | PostgreSQL password | `postgrespw` |
| `postgresql.primary.persistence.size` | Storage size | `5Gi` |

#### MongoDB (Bitnami)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `mongodb.enabled` | Enable MongoDB | `true` |
| `mongodb.auth.rootUser` | Root username | `root` |
| `mongodb.auth.rootPassword` | Root password | `mongopw` |
| `mongodb.persistence.size` | Storage size | `5Gi` |
| `mongodb.image.tag` | MongoDB version | `6.0` |

#### Cassandra (Bitnami)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `cassandra.enabled` | Enable Cassandra | `true` |
| `cassandra.cluster.name` | Cluster name | `TestCluster` |
| `cassandra.persistence.size` | Storage size | `5Gi` |

#### SQL Server

| Parameter | Description | Default |
|-----------|-------------|---------|
| `sqlserver.enabled` | Enable SQL Server | `true` |
| `sqlserver.image.tag` | SQL Server version | `2022-latest` |
| `sqlserver.auth.saPassword` | SA password | `SqlServer2022!` |
| `sqlserver.persistence.size` | Storage size | `5Gi` |

#### OpenSearch

| Parameter | Description | Default |
|-----------|-------------|---------|
| `opensearch.enabled` | Enable OpenSearch | `true` |
| `opensearch.singleNode` | Single node mode | `true` |
| `opensearch.persistence.size` | Storage size | `5Gi` |

## Storage Configuration

By default, all databases use the cluster's default storage class with 5GB storage. To use a specific storage class:

```yaml
postgresql:
  primary:
    persistence:
      storageClass: "gp3"  # AWS example

mongodb:
  persistence:
    storageClass: "gp3"

# Apply to all databases similarly
```

## Accessing the Application

### Via Ingress (Default)

After installation, the application will be available at the configured hostname:
```
https://journal.yourdomain.com
```

Make sure to:
1. Configure DNS to point to your Ingress controller
2. Set up TLS certificates (the chart includes cert-manager annotations)

### Via Port-Forward (Development)

```bash
kubectl port-forward -n journal svc/journal 8080:80
# Access at http://localhost:8080
```

## Database Connections

All databases are accessible within the cluster using these connection strings:

### PostgreSQL
```
Host: journal-postgresql
Port: 5432
Username: postgres
Password: (from values.yaml)
Database: journaldb
```

### MongoDB
```
Host: journal-mongodb
Port: 27017
Username: root
Password: (from values.yaml)
Connection String: mongodb://root:mongopw@journal-mongodb:27017/
```

### Cassandra
```
Host: journal-cassandra
Port: 9042
```

### SQL Server
```
Host: journal-sqlserver
Port: 1433
Username: sa
Password: (from values.yaml)
Connection String: Server=journal-sqlserver,1433;Database=master;User Id=sa;Password=SqlServer2022!;
```

### OpenSearch
```
Host: journal-opensearch
HTTP Port: 9200
Transport Port: 9600
Username: admin
Password: (from values.yaml)
URL: https://journal-opensearch:9200
```

## Upgrading

```bash
# Upgrade with updated values
helm upgrade journal . -n journal

# Upgrade a specific dependency
helm dependency update
helm upgrade journal . -n journal
```

## Uninstalling

```bash
# Uninstall the release
helm uninstall journal -n journal

# Delete the namespace (including PVCs)
kubectl delete namespace journal
```

**Warning**: This will delete all data stored in persistent volumes!

## Troubleshooting

### Check Pod Status
```bash
kubectl get pods -n journal
```

### View Pod Logs
```bash
# Journal application
kubectl logs -n journal -l app.kubernetes.io/name=journal

# SQL Server
kubectl logs -n journal -l app.kubernetes.io/component=sqlserver

# PostgreSQL
kubectl logs -n journal -l app.kubernetes.io/name=postgresql
```

### Check PVC Status
```bash
kubectl get pvc -n journal
```

### Verify Database Connectivity
```bash
# Test PostgreSQL connection
kubectl run -it --rm --restart=Never postgres-client --image=postgres --namespace=journal -- psql -h journal-postgresql -U postgres

# Test MongoDB connection
kubectl run -it --rm --restart=Never mongo-client --image=mongo:6.0 --namespace=journal -- mongosh mongodb://root:mongopw@journal-mongodb:27017/

# Test SQL Server connection
kubectl run -it --rm --restart=Never mssql-client --image=mcr.microsoft.com/mssql-tools --namespace=journal -- /opt/mssql-tools/bin/sqlcmd -S journal-sqlserver -U sa -P 'SqlServer2022!'
```

## Production Recommendations

1. **Change all default passwords** in `values.yaml`
2. **Use Kubernetes Secrets** instead of plaintext passwords
3. **Configure resource limits** based on your workload
4. **Enable backup solutions** for databases
5. **Set up monitoring** (Prometheus, Grafana)
6. **Configure TLS/SSL** for all database connections
7. **Use specific image tags** instead of `latest`
8. **Test disaster recovery procedures**
9. **Implement proper RBAC** policies
10. **Use NetworkPolicies** to restrict traffic

## Chart Structure

```
journal-helm/
├── Chart.yaml              # Chart metadata and dependencies
├── values.yaml             # Default configuration values
├── .helmignore            # Files to ignore when packaging
├── README.md              # This file
├── templates/
│   ├── _helpers.tpl       # Template helpers
│   ├── deployment.yaml    # Journal app deployment
│   ├── service.yaml       # Journal app service
│   ├── ingress.yaml       # Ingress configuration
│   ├── secrets.yaml       # Database passwords secret
│   ├── sqlserver-statefulset.yaml  # SQL Server StatefulSet
│   ├── sqlserver-service.yaml      # SQL Server service
│   └── NOTES.txt          # Post-installation notes
└── charts/                # Dependency charts (generated)
```

## Support

For issues and questions:
- Check pod logs: `kubectl logs -n journal <pod-name>`
- Review events: `kubectl get events -n journal`
- Verify configuration: `helm get values journal -n journal`

## License

This Helm chart is provided as-is for the Journal application deployment.
