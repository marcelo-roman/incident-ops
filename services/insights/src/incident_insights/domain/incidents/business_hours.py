from dataclasses import dataclass
from datetime import datetime
from zoneinfo import ZoneInfo

WEEKEND_START = 5


@dataclass(frozen=True, slots=True)
class BusinessHours:
    timezone: ZoneInfo
    start_hour: int
    end_hour: int

    def __post_init__(self) -> None:
        if not 0 <= self.start_hour < self.end_hour <= 24:
            raise ValueError("business hours must be a range within a day")

    def excludes(self, moment: datetime) -> bool:
        local = moment.astimezone(self.timezone)
        if local.weekday() >= WEEKEND_START:
            return True
        return not self.start_hour <= local.hour < self.end_hour


ON_CALL_BUSINESS_HOURS = BusinessHours(ZoneInfo("America/New_York"), 9, 18)
