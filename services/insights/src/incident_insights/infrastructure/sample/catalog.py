from dataclasses import dataclass


@dataclass(frozen=True)
class IssueFamily:
    title: str
    description: str
    root_cause: str
    severity_weights: tuple[float, float, float, float]
    alert_share: float = 0.5


@dataclass(frozen=True)
class SampleService:
    id: str
    daily_rate: float
    families: tuple[IssueFamily, ...]


@dataclass(frozen=True)
class VolumeSpike:
    service_id: str
    week_offset: int
    multiplier: float


ENGINEERS = (
    "alice.nguyen",
    "bruno.costa",
    "carla.mendes",
    "diego.ramos",
    "erin.walsh",
    "farah.khan",
)

REGIONS = ("eastus2", "westeurope", "brazilsouth", "southeastasia")

SHARED_FAMILY_SHARE = 0.2
ALERTMANAGER_SHARE = 0.55

SHARED_FAMILIES = (
    IssueFamily(
        "TLS certificate expiring on {service} endpoint",
        "The public certificate for {service} expires in {days} days and automated renewal "
        "did not run.",
        "Certificate renewal job was disabled during a pipeline migration.",
        (0.0, 0.1, 0.4, 0.5),
        0.9,
    ),
    IssueFamily(
        "Database connection pool exhausted on {service}",
        "Requests fail with connection pool timeout errors; active connections are pinned at "
        "the maximum of {count}.",
        "Connection leak in an error path that skipped disposing the connection.",
        (0.1, 0.5, 0.4, 0.0),
        0.85,
    ),
    IssueFamily(
        "Error rate spike after deployment of {service}",
        "Error rate rose to {pct}% minutes after a release in {region}; the release was rolled "
        "back.",
        "Regression shipped without canary analysis.",
        (0.1, 0.4, 0.4, 0.1),
        0.8,
    ),
)

SERVICES = (
    SampleService(
        "checkout",
        0.45,
        (
            IssueFamily(
                "Checkout API p99 latency above {latency}ms",
                "Cart and order placement requests are slow in {region}; p99 latency stayed above "
                "{latency}ms for more than ten minutes.",
                "Slow query on the orders table after an index was dropped.",
                (0.05, 0.35, 0.5, 0.1),
                0.9,
            ),
            IssueFamily(
                "Order placement failing with HTTP 500",
                "Customers cannot place orders; POST /orders returns HTTP 500 in {region}.",
                "Null reference in promotion rules after a configuration change.",
                (0.3, 0.5, 0.2, 0.0),
                0.7,
            ),
            IssueFamily(
                "Promo code validation rejecting valid codes",
                "Valid campaign promo codes are rejected as invalid at checkout.",
                "Stale promotion cache after a campaign update.",
                (0.0, 0.2, 0.6, 0.2),
                0.1,
            ),
        ),
    ),
    SampleService(
        "payments-gateway",
        0.4,
        (
            IssueFamily(
                "Card authorization timeouts with acquirer",
                "Authorization requests to the acquirer time out after 30 seconds; payment "
                "success rate dropped to {pct}%.",
                "Acquirer endpoint degradation with no failover to the secondary acquirer.",
                (0.3, 0.5, 0.2, 0.0),
                0.8,
            ),
            IssueFamily(
                "Payment webhook delivery backlog",
                "Payment status webhooks are queued and delayed by {minutes} minutes.",
                "Webhook consumer scaled to zero after an autoscaling rule change.",
                (0.0, 0.3, 0.5, 0.2),
                0.6,
            ),
            IssueFamily(
                "Duplicate charges reported by customers",
                "Customers report being charged twice; the idempotency key is not honored on "
                "retried authorizations.",
                "Client retry policy bypassed the idempotency key.",
                (0.2, 0.5, 0.3, 0.0),
                0.05,
            ),
        ),
    ),
    SampleService(
        "identity",
        0.3,
        (
            IssueFamily(
                "Login failures with invalid token errors",
                "Sign-in fails and sessions are dropped; token validation rejects valid tokens "
                "in {region}.",
                "Signing key rotation was not propagated to every validator.",
                (0.2, 0.5, 0.3, 0.0),
                0.4,
            ),
            IssueFamily(
                "MFA SMS codes not delivered",
                "One-time passcodes sent by SMS are delayed or never delivered to users in "
                "{region}.",
                "SMS provider throttled the sender after a traffic burst.",
                (0.0, 0.4, 0.5, 0.1),
                0.2,
            ),
        ),
    ),
    SampleService(
        "notifications",
        0.35,
        (
            IssueFamily(
                "Email delivery queue backlog",
                "Outbound email queue depth is above {count}; transactional emails are delayed.",
                "Queue consumer crash loop after a dependency upgrade.",
                (0.0, 0.2, 0.6, 0.2),
                0.85,
            ),
            IssueFamily(
                "Push notifications failing for Android devices",
                "Android push notifications fail with invalid credentials from the push provider.",
                "Push provider credentials expired.",
                (0.0, 0.3, 0.5, 0.2),
                0.3,
            ),
        ),
    ),
    SampleService(
        "search",
        0.3,
        (
            IssueFamily(
                "Search results stale after catalog update",
                "Product search shows outdated prices and stock; index lag is {minutes} minutes.",
                "Indexer stuck on a poison message.",
                (0.0, 0.2, 0.6, 0.2),
                0.3,
            ),
            IssueFamily(
                "Search cluster high CPU and slow queries",
                "Search query latency is above {latency}ms with cluster CPU at 95% on data nodes.",
                "Expensive wildcard queries introduced by a new filter.",
                (0.05, 0.35, 0.5, 0.1),
                0.9,
            ),
        ),
    ),
    SampleService(
        "reporting",
        0.2,
        (
            IssueFamily(
                "Nightly reporting ETL job failed",
                "The nightly ETL job failed and dashboards show the previous day's data.",
                "Upstream schema change in the orders export.",
                (0.0, 0.1, 0.5, 0.4),
                0.6,
            ),
            IssueFamily(
                "Report export timeouts for large date ranges",
                "CSV report exports time out for date ranges longer than {days} days.",
                "Unbounded query without pagination.",
                (0.0, 0.0, 0.5, 0.5),
                0.15,
            ),
        ),
    ),
    SampleService(
        "platform",
        0.15,
        (
            IssueFamily(
                "Dead-lettered messages on incident-events topic",
                "Dead-lettered message count on the incident-events topic is above {count}.",
                "Consumer rejected messages carrying an outdated schema version.",
                (0.0, 0.3, 0.5, 0.2),
                0.95,
            ),
            IssueFamily(
                "SLA watchdog function failures",
                "The SLA check function fails on {pct}% of executions in {region}.",
                "Managed identity lost its role assignment on the Service Bus namespace.",
                (0.05, 0.45, 0.4, 0.1),
                0.95,
            ),
            IssueFamily(
                "Availability test failing for incidents API",
                "The availability test on /health/ready fails from {region}.",
                "New revision failed readiness after a configuration change.",
                (0.3, 0.5, 0.2, 0.0),
                1.0,
            ),
        ),
    ),
)

VOLUME_SPIKES = (
    VolumeSpike("payments-gateway", week_offset=7, multiplier=5.0),
    VolumeSpike("search", week_offset=15, multiplier=4.0),
)
