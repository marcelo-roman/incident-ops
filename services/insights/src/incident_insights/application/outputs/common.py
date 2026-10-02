from datetime import datetime
from typing import Self

from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel

from incident_insights.domain.shared.duration_stats import DurationStats
from incident_insights.domain.shared.percentage import Percentage
from incident_insights.domain.shared.reporting_window import ReportingWindow


class Output(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, frozen=True)


class WindowOutput(Output):
    start: datetime
    end: datetime
    days: int

    @classmethod
    def of(cls, window: ReportingWindow) -> Self:
        return cls(start=window.start, end=window.end, days=window.days)


class DurationStatsOutput(Output):
    samples: int
    median_minutes: float | None
    p90_minutes: float | None

    @classmethod
    def of(cls, stats: DurationStats) -> Self:
        return cls(
            samples=stats.samples,
            median_minutes=stats.median_minutes,
            p90_minutes=stats.p90_minutes,
        )


def percent(value: Percentage | None) -> float | None:
    if value is None:
        return None
    return value.value
