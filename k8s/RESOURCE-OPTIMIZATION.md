# Resource Optimization for 4CPU/4GB Nodes

This document explains the resource optimization performed for the Journal Helm chart to fit on worker nodes with only 4 CPU cores and 4GB RAM.

## Node Resource Constraints

**Available per node:**
- CPU: 4 cores (4000m)
- RAM: 4GB (4096Mi)

**Reserved for Kubernetes system pods** (kubelet, kube-proxy, kube-dns, etc):
- CPU: ~500-700m
- RAM: ~500-700Mi

**Available for workloads:**
- CPU: ~3300m (3.3 cores)
- RAM: ~3300Mi (~3.2GB)

---

## Resource Allocation Comparison

### Before Optimization (WOULD NOT FIT!)

| Component | CPU Request | CPU Limit | RAM Request | RAM Limit |
|-----------|-------------|-----------|-------------|-----------|
| Journal App | 250m | 1000m | 256Mi | 512Mi |
| SQL Server | 500m | 2000m | 1Gi (1024Mi) | 2Gi (2048Mi) |
| PostgreSQL | 250m | 1000m | 256Mi | 512Mi |
| MongoDB | 250m | 1000m | 256Mi | 512Mi |
| Cassandra | 500m | 2000m | 1Gi (1024Mi) | 2Gi (2048Mi) |
| OpenSearch | 500m | 1000m | 512Mi | 1Gi (1024Mi) |
| **TOTAL** | **2250m** | **8000m** | **3328Mi** | **6656Mi** |

**Problem:** Requests alone (2250m CPU, 3328Mi RAM) exceed available resources on a 4GB node!

---

### After Optimization (FITS COMFORTABLY!)

| Component | CPU Request | CPU Limit | RAM Request | RAM Limit |
|-----------|-------------|-----------|-------------|-----------|
| Journal App | 100m | 500m | 128Mi | 256Mi |
| SQL Server | 250m | 1000m | 512Mi | 1Gi (1024Mi) |
| PostgreSQL | 100m | 400m | 128Mi | 256Mi |
| MongoDB | 100m | 500m | 256Mi | 512Mi |
| Cassandra | 250m | 1000m | 512Mi | 1Gi (1024Mi) |
| OpenSearch | 200m | 600m | 384Mi | 768Mi |
| **TOTAL** | **1000m** | **4000m** | **1920Mi** | **3840Mi** |

**Result:**
- ✅ Total requests: 1000m CPU (30% of node), 1920Mi RAM (58% of 3.3GB available)
- ✅ Total limits: 4000m CPU (100% burstable), 3840Mi RAM (94% of available)
- ✅ Leaves ~2300m CPU and ~1380Mi RAM for other workloads and headroom
- ✅ All components can run on a single 4CPU/4GB node

---

## Optimization Details

### 1. Journal Application
- **Before:** 250m/256Mi requests, 1000m/512Mi limits
- **After:** 100m/128Mi requests, 500m/256Mi limits
- **Reasoning:** Lightweight ASP.NET Core app, doesn't need much for basic operations

### 2. SQL Server
- **Before:** 500m/1Gi requests, 2000m/2Gi limits
- **After:** 250m/512Mi requests, 1000m/1Gi limits
- **Reasoning:** Reduced but still functional. SQL Server needs memory for caching, but 512Mi-1Gi is workable for small workloads

### 3. PostgreSQL
- **Before:** 250m/256Mi requests, 1000m/512Mi limits
- **After:** 100m/128Mi requests, 400m/256Mi limits
- **Reasoning:** Currently not used by the application, so minimal resources allocated

### 4. MongoDB
- **Before:** 250m/256Mi requests, 1000m/512Mi limits
- **After:** 100m/256Mi requests, 500m/512Mi limits
- **Reasoning:** MongoDB uses memory for working set. Increased memory request to 256Mi for better performance

### 5. Cassandra
- **Before:** 500m/1Gi requests, 2000m/2Gi limits
- **After:** 250m/512Mi requests, 1000m/1Gi limits
- **Reasoning:** Cassandra is memory-intensive, but can run with 512Mi-1Gi for small datasets

### 6. OpenSearch
- **Before:** 500m/512Mi requests, 1000m/1Gi limits (Java heap: 512m)
- **After:** 200m/384Mi requests, 600m/768Mi limits (Java heap: 384m)
- **Reasoning:** Reduced Java heap from 512m to 384m to match memory limits. OpenSearch needs ~50% overhead for OS caching

---

## Performance Considerations

### What These Resources Support

**✅ Development and Testing:**
- Small to medium datasets
- 1-5 concurrent users
- Development workflows
- Integration testing

**✅ Small Production Workloads:**
- Light traffic applications
- Microservices with limited data
- Proof-of-concept deployments

### Performance Impact

| Component | Impact | Mitigation |
|-----------|--------|----------|
| Journal App | Minimal - app is lightweight | Good for most use cases |
| SQL Server | Moderate - reduced cache size | Use persistent storage, optimize queries |
| PostgreSQL | None - not used by app | Can be disabled if needed |
| MongoDB | Low - adequate for small datasets | Good for typical workloads |
| Cassandra | Moderate - limited memory | Use for smaller datasets, optimize schema |
| OpenSearch | Moderate - smaller heap | Index fewer documents, optimize mappings |

---

## Scaling Options

### Option 1: Disable Unused Databases

If you don't need PostgreSQL (not used by app), disable it:

```yaml
# values.yaml
postgresql:
  enabled: false
```

**Savings:** 100m CPU, 128Mi RAM

### Option 2: Run Databases on Separate Nodes

Use node selectors to separate databases from application:

```yaml
journal:
  nodeSelector:
    node-type: application

sqlserver:
  nodeSelector:
    node-type: database
```

### Option 3: Use External Managed Databases

For production, consider using cloud-managed databases:
- AWS RDS (SQL Server, PostgreSQL)
- MongoDB Atlas
- AWS OpenSearch Service
- AWS Keyspaces (Cassandra)

Disable in-cluster databases:
```yaml
postgresql:
  enabled: false
mongodb:
  enabled: false
cassandra:
  enabled: false
sqlserver:
  enabled: false
opensearch:
  enabled: false
```

**Savings:** All database resources, only run the journal app

### Option 4: Vertical Node Scaling

If performance is inadequate, upgrade nodes to:
- **Recommended:** 8 CPU / 8GB RAM (2x current)
- **Ideal:** 16 CPU / 16GB RAM (4x current)

---

## Monitoring Resource Usage

### Check Resource Utilization

```bash
# Check pod resource usage
kubectl top pods -n journal

# Check node resource usage
kubectl top nodes

# Describe node to see allocated resources
kubectl describe node <node-name>
```

### Watch for Resource Pressure

```bash
# Check for OOMKilled pods (out of memory)
kubectl get pods -n journal -o json | jq '.items[] | select(.status.containerStatuses[]?.lastState.terminated.reason == "OOMKilled")'

# Check for evicted pods
kubectl get pods -n journal | grep Evicted

# View pod events
kubectl get events -n journal --sort-by='.lastTimestamp'
```

### Signs You Need More Resources

**Memory Issues:**
- Pods being OOMKilled
- Frequent pod restarts
- Slow database queries
- OpenSearch heap errors

**CPU Issues:**
- High latency in application responses
- Slow database operations
- Pods in CrashLoopBackOff

---

## Resource Optimization Tips

### 1. Disable PostgreSQL if Not Used

```yaml
postgresql:
  enabled: false
```

### 2. Reduce Number of Databases

If your app doesn't need all databases, disable unused ones:

```yaml
cassandra:
  enabled: false  # If not using Cassandra
opensearch:
  enabled: false  # If not using search
```

### 3. Use PodDisruptionBudgets

Prevent accidental evictions:

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: journal-pdb
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app.kubernetes.io/name: journal
```

### 4. Enable HorizontalPodAutoscaler for Journal App

Scale journal app pods based on CPU:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: journal-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: journal
  minReplicas: 1
  maxReplicas: 3
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

### 5. Optimize Database Connections

Configure connection pooling in application:
- SQL Server: Max pool size = 50
- MongoDB: Max pool size = 50
- Cassandra: Connection pool size = 5

---

## Testing Resource Configuration

### Load Testing

```bash
# Install Apache Bench
apt-get install apache2-utils

# Test journal app (100 requests, 10 concurrent)
ab -n 100 -c 10 http://journal.example.com/

# Monitor resources during test
watch kubectl top pods -n journal
```

### Database Stress Testing

```bash
# SQL Server - create test database
kubectl exec -it journal-sqlserver-0 -n journal -- \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'SqlServer2022!' \
  -Q "CREATE DATABASE testdb"

# MongoDB - insert test data
kubectl exec -it journal-mongodb-0 -n journal -- \
  mongosh mongodb://root:mongopw@localhost:27017/ \
  --eval "db.test.insertMany(Array.from({length: 1000}, (_, i) => ({id: i, data: 'test'})))"
```

---

## Troubleshooting

### Pod Stuck in Pending State

```bash
# Check why pod is pending
kubectl describe pod <pod-name> -n journal

# Common reasons:
# - Insufficient CPU: Reduce CPU requests
# - Insufficient memory: Reduce memory requests
# - No nodes available: Add more nodes or reduce requests
```

### OOMKilled Pods

```bash
# Increase memory limits
# In values.yaml, increase memory.limits for the affected component

# Or disable a database to free up memory
postgresql:
  enabled: false
```

### Slow Performance

```bash
# Check if hitting CPU limits
kubectl top pods -n journal

# If CPU usage is at limit, increase CPU limits or reduce concurrent operations
```

---

## Summary

The optimized configuration:
- ✅ Fits on 4CPU/4GB nodes
- ✅ Leaves headroom for system pods
- ✅ Suitable for development and small production
- ✅ Can scale vertically or horizontally as needed
- ✅ Reduced costs while maintaining functionality

**Next Steps:**
1. Deploy and monitor resource usage
2. Adjust based on actual workload
3. Consider disabling unused databases
4. Plan for scaling if workload grows
