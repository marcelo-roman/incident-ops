using IncidentOps.Domain.Incidents;

namespace IncidentOps.Infrastructure.Seeding;

public static class IncidentThemeCatalog
{
    public static readonly IncidentTheme PaymentProviderTimeout = new(
        "Payment provider timeout",
        14,
        ["payments-gateway", "checkout"],
        Weights(2, 5, 3, 1),
        [
            "Payment provider timeout on card authorization",
            "Timeouts calling payment provider during checkout",
            "Card authorization latency above 5s",
            "Payment provider returning gateway timeouts",
            "Elevated 504s from payment provider",
            "Payment capture requests timing out",
        ],
        [
            "Authorization p95 latency crossed 5s and timeouts are surfacing to customers at checkout.",
            "Error-rate alert on payment provider calls; retries are exhausting the outbound connection pool.",
            "Synthetic checkout journey failing at the payment step with upstream timeouts.",
        ],
        [
            "Provider status page reports degraded performance in one region.",
            "Opened a ticket with the payment provider support desk.",
            "Retry budget is saturating the outbound HTTP pool.",
        ],
        [
            "Failed over to the secondary payment provider region.",
            "Reduced authorization timeout and enabled circuit breaker on provider calls.",
            "Shifted card traffic to the backup acquirer.",
        ],
        [
            "Upstream payment provider degraded in one region; retries without jitter exhausted the connection pool.",
            "Missing timeout budget on authorization calls amplified provider latency into checkout failures.",
            "Provider-side maintenance window not announced; no automatic regional failover configured.",
        ]);

    public static readonly IncidentTheme CertificateExpiry = new(
        "Certificate expiry",
        6,
        ["identity", "payments-gateway", "notifications"],
        Weights(2, 4, 3, 1),
        [
            "TLS certificate expired on public endpoint",
            "Expired client certificate for payment provider mTLS",
            "Certificate expiry causing TLS handshake failures",
            "SMTP relay certificate expired",
            "Certificate renewal job failed silently",
        ],
        [
            "Clients report TLS handshake failures; the presented certificate expired at midnight UTC.",
            "Outbound mTLS calls rejected by the partner because the client certificate expired.",
            "Certificate monitor did not alert; failures detected by customer reports.",
        ],
        [
            "Confirmed the certificate expired and the renewal pipeline last ran 45 days ago.",
            "Renewal job credentials were rotated and never updated.",
            "Checking every endpoint that shares the same certificate chain.",
        ],
        [
            "Issued and deployed a new certificate manually.",
            "Rolled the renewed certificate to every ingress.",
            "Temporarily routed traffic through the gateway with a valid certificate.",
        ],
        [
            "Automated renewal failed after a credential rotation and the expiry alert was routed to an unused channel.",
            "Certificate tracked in a spreadsheet instead of the certificate inventory; renewal was missed.",
            "Renewal succeeded but the new certificate was never bound to the listener.",
        ]);

    public static readonly IncidentTheme QueueBacklog = new(
        "Queue backlog",
        12,
        ["notifications", "reporting"],
        Weights(0, 3, 5, 3),
        [
            "Notification queue backlog above 50k messages",
            "Email dispatch delayed due to queue backlog",
            "Report generation queue not draining",
            "Consumer lag on notification queue",
            "Dead-letter queue growing on notifications",
            "Queue depth alert on scheduled reports",
        ],
        [
            "Queue depth growing steadily for 30 minutes; consumers are processing below the arrival rate.",
            "Customers report delayed emails; oldest message age is above the 10 minute objective.",
            "Dead-letter count increasing after a payload schema change.",
        ],
        [
            "Consumers are healthy but throughput dropped after the last release.",
            "Poison messages are being retried until max delivery count.",
            "Arrival rate doubled after a marketing campaign launch.",
        ],
        [
            "Scaled out consumers to drain the backlog.",
            "Moved poison messages to quarantine and resumed processing.",
            "Paused the bulk producer until the queue drained.",
        ],
        [
            "Consumer prefetch misconfigured in the last release, limiting throughput to a single message at a time.",
            "Payload schema change produced messages the consumer could not deserialize.",
            "Autoscaling rule keyed on CPU instead of queue depth did not react to the burst.",
        ]);

    public static readonly IncidentTheme MemoryLeakAfterDeploy = new(
        "Memory leak after deploy",
        11,
        ["search", "checkout", "reporting"],
        Weights(1, 4, 5, 2),
        [
            "Memory leak after deploy causing pod restarts",
            "OOM kills after latest release",
            "Memory growth after deploy degrading latency",
            "Worker memory leak after release",
            "Container restarts due to memory pressure after deploy",
        ],
        [
            "Working set grows linearly since the last deployment and pods restart every 40 minutes.",
            "Out-of-memory kills detected on several replicas right after the release.",
            "Latency rising with GC pause time after the latest rollout.",
        ],
        [
            "Heap dump shows a static cache growing without eviction.",
            "Restarts correlate with the deploy timestamp.",
            "Comparing allocation profile with the previous build.",
        ],
        [
            "Rolled back to the previous release.",
            "Increased memory limit and scheduled rolling restarts until the fix ships.",
            "Disabled the feature flag that introduced the cache.",
        ],
        [
            "New in-memory cache had no size limit or eviction policy.",
            "HttpClient instances created per request leaked sockets and buffers.",
            "Event handler subscriptions were never removed, keeping request objects alive.",
        ]);

    public static readonly IncidentTheme DnsResolutionFailures = new(
        "DNS resolution failures",
        8,
        ["checkout", "payments-gateway", "identity", "notifications", "search", "reporting"],
        Weights(2, 4, 3, 1),
        [
            "DNS resolution failures for internal services",
            "Intermittent NXDOMAIN for database host",
            "DNS lookups timing out from the cluster",
            "Service discovery failing due to DNS errors",
            "Name resolution errors calling downstream APIs",
        ],
        [
            "Intermittent name resolution errors across several workloads in the same cluster.",
            "Calls to the database host fail with NXDOMAIN for a fraction of requests.",
            "DNS query latency spiked and requests time out before the connection is attempted.",
        ],
        [
            "Resolver pods are CPU throttled.",
            "A private DNS zone link was removed during a network change.",
            "Errors only appear on nodes in one availability zone.",
        ],
        [
            "Scaled resolver replicas and raised CPU limits.",
            "Restored the private DNS zone link.",
            "Added node-local DNS cache to the affected node pool.",
        ],
        [
            "Resolver capacity was sized for half of the current query volume.",
            "Infrastructure change removed a private DNS zone link without a plan review.",
            "Search domain expansion multiplied queries per lookup by five.",
        ]);

    public static readonly IncidentTheme ConnectionPoolExhaustion = new(
        "Database connection pool exhaustion",
        8,
        ["checkout", "identity", "payments-gateway"],
        Weights(1, 4, 4, 1),
        [
            "Database connection pool exhausted",
            "Timeouts acquiring SQL connections",
            "Login failures due to connection pool exhaustion",
            "Order writes failing with pool timeout",
        ],
        [
            "Requests fail with timeouts while acquiring a connection from the pool.",
            "Error rate rising with active connections pinned at the pool maximum.",
            "Database CPU is low but the application cannot obtain connections.",
        ],
        [
            "A long-running query holds connections for minutes.",
            "Connections are not returned on an exception path.",
            "Traffic is normal for this time of day.",
        ],
        [
            "Killed the long-running sessions and recycled the application pool.",
            "Raised the pool size temporarily.",
            "Disabled the batch job competing for connections.",
        ],
        [
            "Connection leak on an exception path that skipped disposal.",
            "Batch job shared the pool with interactive traffic and held connections for minutes.",
            "Missing index turned a hot query into a table scan holding connections.",
        ]);

    public static readonly IncidentTheme SearchIndexLag = new(
        "Search index lag",
        7,
        ["search"],
        Weights(0, 1, 5, 4),
        [
            "Search index lagging behind catalog updates",
            "Stale search results after indexing failure",
            "Indexer stuck on a malformed document",
            "Search freshness objective breached",
        ],
        [
            "Products updated in the catalog are not visible in search after 30 minutes.",
            "Indexer throughput dropped to zero; last successful batch two hours ago.",
            "Customers report search showing prices that changed this morning.",
        ],
        [
            "Indexer is retrying the same batch.",
            "Change feed checkpoint has not moved.",
            "Index replicas are healthy; ingestion is the bottleneck.",
        ],
        [
            "Skipped the malformed document and restarted the indexer.",
            "Reset the change feed checkpoint and backfilled the gap.",
            "Triggered a partial reindex for the affected categories.",
        ],
        [
            "Malformed document blocked the batch because the indexer had no dead-letter path.",
            "Change feed lease expired and was not renewed after a deploy.",
            "Indexer throttled by the search service tier during peak catalog updates.",
        ]);

    public static readonly IncidentTheme AuthenticationFailures = new(
        "Authentication failures",
        7,
        ["identity"],
        Weights(1, 4, 4, 1),
        [
            "Elevated login failures",
            "Token issuance latency spike",
            "SSO callback errors for enterprise tenants",
            "Refresh token validation failing",
        ],
        [
            "Login success rate dropped below 95% for the last 15 minutes.",
            "Token endpoint p95 above 3s; clients retry and amplify load.",
            "Enterprise tenants report SSO callback errors after a configuration change.",
        ],
        [
            "Signing key rotation happened 20 minutes before the first error.",
            "Errors concentrated on a single identity provider.",
            "Cache hit ratio for JWKS dropped to zero.",
        ],
        [
            "Republished the previous signing key alongside the new one.",
            "Reverted the identity provider configuration change.",
            "Scaled token service replicas.",
        ],
        [
            "Signing key rotated without overlap, invalidating tokens still in use.",
            "Identity provider metadata URL changed and the cached metadata expired.",
            "JWKS cache keyed per request, sending every validation to the key endpoint.",
        ]);

    public static readonly IncidentTheme DiskSpace = new(
        "Disk space exhausted",
        5,
        ["reporting", "search"],
        Weights(0, 1, 4, 5),
        [
            "Disk space exhausted on reporting worker",
            "Report export failing with disk full",
            "Log volume filling node disk",
            "Temporary files not cleaned up on worker",
        ],
        [
            "Workers fail writes with no space left on device.",
            "Exports fail midway; temporary directory is at 100% usage.",
            "Node disk pressure evicting pods.",
        ],
        [
            "Temporary export files older than a week found on disk.",
            "Debug logging was left on after the last investigation.",
            "Disk usage grows about 8% per day.",
        ],
        [
            "Cleaned temporary files and restarted the workers.",
            "Turned debug logging off and rotated logs.",
            "Expanded the volume.",
        ],
        [
            "Export job did not delete temporary files on failure paths.",
            "Debug logging enabled in production without an expiry.",
            "Log rotation not configured for the worker container.",
        ]);

    public static readonly IncidentTheme ThirdPartyRateLimiting = new(
        "Third-party rate limiting",
        6,
        ["notifications"],
        Weights(0, 2, 5, 3),
        [
            "SMS provider rate limiting outbound messages",
            "Push notification provider returning 429",
            "Email provider throttling sends",
            "Rate limit exceeded on messaging provider",
        ],
        [
            "Outbound messages rejected with 429 responses from the provider.",
            "Send rate capped by the provider; delivery delays growing.",
            "Provider quota reached earlier than usual in the day.",
        ],
        [
            "Campaign send started at the same time as transactional traffic.",
            "Retry policy is not honoring Retry-After.",
            "Provider confirmed our account quota.",
        ],
        [
            "Throttled campaign sends and prioritized transactional messages.",
            "Enabled Retry-After aware backoff.",
            "Requested a temporary quota increase from the provider.",
        ],
        [
            "Campaign and transactional traffic shared one provider quota without prioritization.",
            "Retries ignored Retry-After and multiplied rejected requests.",
            "Quota sized for last year's traffic and never reviewed.",
        ]);

    public static readonly IncidentTheme SlowQueries = new(
        "Slow queries",
        6,
        ["reporting", "checkout"],
        Weights(0, 2, 5, 3),
        [
            "Slow query on order history",
            "Dashboard queries timing out",
            "Query plan regression after statistics update",
            "Reporting database CPU at 100%",
        ],
        [
            "Order history page p95 above 8s.",
            "Dashboard tiles fail with command timeouts.",
            "Database CPU saturated by a single query shape.",
        ],
        [
            "Query store shows a plan change this morning.",
            "Missing index suggestion on the orders table.",
            "Ad-hoc export running against the primary.",
        ],
        [
            "Forced the previous query plan.",
            "Created the missing index online.",
            "Moved the export to the read replica.",
        ],
        [
            "Plan regression after automatic statistics update, with no plan forcing for the hot query.",
            "New filter added without a supporting index.",
            "Analytical exports pointed at the primary instead of the replica.",
        ]);

    public static IReadOnlyList<IncidentTheme> All { get; } =
    [
        PaymentProviderTimeout,
        CertificateExpiry,
        QueueBacklog,
        MemoryLeakAfterDeploy,
        DnsResolutionFailures,
        ConnectionPoolExhaustion,
        SearchIndexLag,
        AuthenticationFailures,
        DiskSpace,
        ThirdPartyRateLimiting,
        SlowQueries,
    ];

    private static Dictionary<Severity, int> Weights(int sev1, int sev2, int sev3, int sev4) => new()
    {
        [Severity.Sev1] = sev1,
        [Severity.Sev2] = sev2,
        [Severity.Sev3] = sev3,
        [Severity.Sev4] = sev4,
    };
}
