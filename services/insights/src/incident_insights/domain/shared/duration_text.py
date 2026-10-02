from datetime import timedelta

SECONDS_PER_MINUTE = 60
MINUTES_PER_HOUR = 60
HOURS_PER_DAY = 24


def describe_duration(duration: timedelta) -> str:
    minutes = max(int(duration.total_seconds() // SECONDS_PER_MINUTE), 0)
    hours, minutes = divmod(minutes, MINUTES_PER_HOUR)
    days, hours = divmod(hours, HOURS_PER_DAY)
    parts = [
        f"{value}{unit}" for value, unit in ((days, "d"), (hours, "h"), (minutes, "m")) if value
    ]
    return " ".join(parts) or "0m"
