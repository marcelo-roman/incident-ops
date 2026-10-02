from datetime import UTC, datetime

from incident_insights.application.ports import Clock


def utc_now() -> datetime:
    return datetime.now(UTC)


def fixed_clock(moment: datetime) -> Clock:
    return lambda: moment
