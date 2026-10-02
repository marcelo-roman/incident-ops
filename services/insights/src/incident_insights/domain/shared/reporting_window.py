from dataclasses import dataclass
from datetime import UTC, date, datetime, timedelta

WEEK = timedelta(days=7)


@dataclass(frozen=True, slots=True)
class ReportingWindow:
    start: datetime
    end: datetime

    def __post_init__(self) -> None:
        if self.start.tzinfo is None or self.end.tzinfo is None:
            raise ValueError("window bounds must be timezone-aware")
        if self.start >= self.end:
            raise ValueError("window start must precede its end")

    @classmethod
    def trailing(cls, days: int, as_of: datetime) -> "ReportingWindow":
        if days < 1:
            raise ValueError("a window spans at least one day")
        return cls(start=as_of - timedelta(days=days), end=as_of)

    @property
    def days(self) -> int:
        return (self.end - self.start).days

    def contains(self, moment: datetime) -> bool:
        return self.start <= moment < self.end

    def week_starts(self) -> tuple[date, ...]:
        first = week_start_of(self.start)
        last = week_start_of(self.end)
        weeks = (last - first).days // 7
        return tuple(first + WEEK * offset for offset in range(weeks + 1))


def week_start_of(moment: datetime) -> date:
    day = moment.astimezone(UTC).date()
    return day - timedelta(days=day.weekday())
