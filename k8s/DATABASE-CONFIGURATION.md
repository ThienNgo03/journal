# Database Configuration Guide

This guide explains how to configure database connections for the Journal application in Kubernetes.

## Overview

The Journal application connects to 5 databases:
1. **SQL Server** - For JournalDb and IdentityDb
2. **MongoDB** - For Journal data
3. **Cassandra** - For distributed data
4. **OpenSearch** - For search functionality
5. **PostgreSQL** - Deployed but not currently used by the app

## Configuration Files

### 1. ConfigMap (templates/configmap.yaml)

The `configmap.yaml` file contains the `appsettings.Docker.json` configuration that the Journal app reads at runtime. This is where all database connection strings are defined.

**Location:** `journal-helm/templates/configmap.yaml`

**Key sections:**

```yaml
data:
  appsettings.Docker.json: |
    {
      "JournalDb": {
        "Host": "{{ include "journal.fullname" . }}-sqlserver",
        "Port": "1433",
        "Database": "ssto-database",
        "Username": "sa",
        "Password": "{{ .Values.sqlserver.auth.saPassword }}"
      },
      "IdentityDb": {
        "Host": "{{ include "journal.fullname" . }}-sqlserver",
        ...
      },
      "OpenSearch": {
        "Host": "{{ .Release.Name }}-opensearch",
        ...
      },
      "MongoDb": {
        "Host": "{{ .Release.Name }}-mongodb",
        ...
      },
      "CassandraDb": {
        "ContactPoint": "{{ .Release.Name }}-cassandra",
        ...
      }
    }
```

### 2. Values File (values.yaml)

The `values.yaml` file contains the database passwords and configuration that get templated into the ConfigMap.

**Location:** `journal-helm/values.yaml`

## Database Connection Details

### SQL Server (JournalDb & IdentityDb)

**Configuration in ConfigMap:**
```json
"JournalDb": {
  "Host": "{{ include "journal.fullname" . }}-sqlserver",
  "Port": "1433",
  "Database": "ssto-database",
  "Username": "sa",
  "Password": "{{ .Values.sqlserver.auth.saPassword }}"
}
```

**Where to change:**
- **Host**: Automatically set to `<release-name>-sqlserver`
- **Password**: In `values.yaml` → `sqlserver.auth.saPassword`
- **Database**: In `templates/configmap.yaml` → `JournalDb.Database`

**Service Name in Cluster:**
```
<release-name>-sqlserver:1433
Example: journal-sqlserver:1433
```

---

### MongoDB

**Configuration in ConfigMap:**
```json
"MongoDb": {
  "Host": "{{ .Release.Name }}-mongodb",
  "Port": 27017,
  "Database": "Journal",
  "Username": "root",
  "Password": "{{ .Values.mongodb.auth.rootPassword }}",
  "AuthDatabase": "admin"
}
```

**Where to change:**
- **Host**: Automatically set to `<release-name>-mongodb`
- **Password**: In `values.yaml` → `mongodb.auth.rootPassword`
- **Database**: In `templates/configmap.yaml` → `MongoDb.Database`

**Service Name in Cluster:**
```
<release-name>-mongodb:27017
Example: journal-mongodb:27017
```

**Connection String Format:**
```
mongodb://root:<password>@journal-mongodb:27017/Journal?authSource=admin
```

---

### Cassandra

**Configuration in ConfigMap:**
```json
"CassandraDb": {
  "ContactPoint": "{{ .Release.Name }}-cassandra",
  "Port": "9042",
  "Keyspace": "journal",
  "DataCenter": "{{ .Values.cassandra.cluster.datacenter }}"
}
```

**Where to change:**
- **ContactPoint**: Automatically set to `<release-name>-cassandra`
- **DataCenter**: In `values.yaml` → `cassandra.cluster.datacenter`
- **Keyspace**: In `templates/configmap.yaml` → `CassandraDb.Keyspace`

**Service Name in Cluster:**
```
<release-name>-cassandra:9042
Example: journal-cassandra:9042
```

---

### OpenSearch

**Configuration in ConfigMap:**
```json
"OpenSearch": {
  "Host": "{{ .Release.Name }}-opensearch",
  "Port": 9200,
  "Username": "admin",
  "Password": "{{ (index .Values.opensearch.extraEnvs 0).value }}"
}
```

**Where to change:**
- **Host**: Automatically set to `<release-name>-opensearch`
- **Password**: In `values.yaml` → `opensearch.extraEnvs[0].value`

**Service Name in Cluster:**
```
<release-name>-opensearch:9200
Example: journal-opensearch:9200
```

**Full URL:**
```
http://journal-opensearch:9200
```

---

## How to Modify Database Connections

### Method 1: Change values.yaml (Recommended)

Edit `values.yaml` to change passwords and basic settings:

```yaml
sqlserver:
  auth:
    saPassword: "YourNewPassword123!"

mongodb:
  auth:
    rootPassword: "YourMongoPassword"

opensearch:
  extraEnvs:
    - name: OPENSEARCH_INITIAL_ADMIN_PASSWORD
      value: "YourOpenSearchPassword123!"
```

Then upgrade your release:
```bash
helm upgrade journal . -n journal
```

### Method 2: Modify ConfigMap Template

For advanced changes (database names, hosts, ports), edit `templates/configmap.yaml`:

```yaml
data:
  appsettings.Docker.json: |
    {
      "JournalDb": {
        "Database": "my-custom-database",  # Change database name
        "Port": "1433"
      }
    }
```

### Method 3: Use Custom Values File

Create a custom values file:

```yaml
# custom-db-config.yaml
sqlserver:
  auth:
    saPassword: "ProductionPassword123!"

mongodb:
  auth:
    rootPassword: "ProductionMongoPass"
```

Install with custom values:
```bash
helm install journal . -n journal -f custom-db-config.yaml
```

### Method 4: Override at Install Time

Override specific values during installation:

```bash
helm install journal . -n journal \
  --set sqlserver.auth.saPassword="MyPassword123!" \
  --set mongodb.auth.rootPassword="MongoPass"
```

---

## Connection String Formats

### SQL Server Connection String (C#)
```csharp
Server=journal-sqlserver,1433;
Database=ssto-database;
User Id=sa;
Password=SqlServer2022!;
TrustServerCertificate=True;
```

### MongoDB Connection String
```
mongodb://root:mongopw@journal-mongodb:27017/Journal?authSource=admin
```

### Cassandra Connection
```csharp
// Contact Point: journal-cassandra
// Port: 9042
// Keyspace: journal
// DataCenter: datacenter1
```

### OpenSearch URL
```
http://admin:MySecure@Pass123!@journal-opensearch:9200
```

---

## Environment Variables

The application uses `ASPNETCORE_ENVIRONMENT=Docker` to load the correct configuration file:

```yaml
# In deployment.yaml
env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Docker"  # Loads appsettings.Docker.json
```

---

## Troubleshooting

### Issue: Application can't connect to database

**Check service names:**
```bash
kubectl get svc -n journal
```

Expected services:
- `journal-sqlserver` (port 1433)
- `journal-mongodb` (port 27017)
- `journal-cassandra` (port 9042)
- `journal-opensearch` (port 9200)

**Check ConfigMap:**
```bash
kubectl get configmap journal-config -n journal -o yaml
```

**Test database connectivity:**
```bash
# Test SQL Server
kubectl run -it --rm test-mssql --image=mcr.microsoft.com/mssql-tools -n journal -- \
  /opt/mssql-tools/bin/sqlcmd -S journal-sqlserver -U sa -P 'SqlServer2022!'

# Test MongoDB
kubectl run -it --rm test-mongo --image=mongo:6.0 -n journal -- \
  mongosh mongodb://root:mongopw@journal-mongodb:27017/

# Test Cassandra
kubectl run -it --rm test-cass --image=cassandra:latest -n journal -- \
  cqlsh journal-cassandra 9042
```

### Issue: Wrong password in ConfigMap

The ConfigMap pulls passwords from `values.yaml`. If you change `values.yaml`, you must upgrade the release:

```bash
helm upgrade journal . -n journal
```

Then restart the journal pods:
```bash
kubectl rollout restart deployment/journal -n journal
```

### Issue: Database not ready

Check if database pods are running:
```bash
kubectl get pods -n journal
```

Wait for all pods to be in `Running` state and `READY 1/1`.

---

## Security Best Practices

1. **Use Kubernetes Secrets instead of ConfigMap for passwords:**
   ```bash
   kubectl create secret generic db-passwords -n journal \
     --from-literal=sqlserver-password='SecurePass123!' \
     --from-literal=mongodb-password='SecureMongoPass'
   ```

2. **Use environment variables for secrets:**
   ```yaml
   env:
     - name: SQLSERVER_PASSWORD
       valueFrom:
         secretKeyRef:
           name: db-passwords
           key: sqlserver-password
   ```

3. **Enable TLS/SSL for database connections** (production)

4. **Rotate passwords regularly**

5. **Use Azure Key Vault or AWS Secrets Manager** for production

---

## Quick Reference

| Database | Service Name | Port | Config Location |
|----------|-------------|------|-----------------|
| SQL Server | `<release>-sqlserver` | 1433 | `values.yaml` → `sqlserver.auth.saPassword` |
| MongoDB | `<release>-mongodb` | 27017 | `values.yaml` → `mongodb.auth.rootPassword` |
| Cassandra | `<release>-cassandra` | 9042 | `values.yaml` → `cassandra.cluster.datacenter` |
| OpenSearch | `<release>-opensearch` | 9200 | `values.yaml` → `opensearch.extraEnvs[0].value` |
| PostgreSQL | `<release>-postgresql` | 5432 | `values.yaml` → `postgresql.auth.postgresPassword` |

---

## Files to Modify

1. **For passwords**: `values.yaml`
2. **For connection strings**: `templates/configmap.yaml`
3. **For deployment environment**: `templates/deployment.yaml`

After any changes, run:
```bash
helm upgrade journal . -n journal
kubectl rollout restart deployment/journal -n journal
```
