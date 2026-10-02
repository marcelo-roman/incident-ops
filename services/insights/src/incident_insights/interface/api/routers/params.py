from typing import Annotated

from fastapi import Query

MAX_DAYS = 730

Days = Annotated[int, Query(ge=1, le=MAX_DAYS, description="Trailing window in days")]
