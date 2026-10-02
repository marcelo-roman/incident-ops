from datetime import UTC, datetime

import typer


def parse_moment(value: str | None) -> datetime | None:
    if value is None:
        return None
    try:
        moment = datetime.fromisoformat(value)
    except ValueError as error:
        raise typer.BadParameter(f"'{value}' is not an ISO-8601 date or timestamp") from error
    if moment.tzinfo is None:
        return moment.replace(tzinfo=UTC)
    return moment.astimezone(UTC)
