from collections.abc import Sequence
from pathlib import Path

import pandas as pd

from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.infrastructure.incidents.acl import IncidentPayload, IncidentTranslator

CSV_FIELDS = set(IncidentPayload.model_fields)


def write_incidents_csv(incidents: Sequence[IncidentRecord], path: Path) -> None:
    translator = IncidentTranslator()
    columns = [field.alias or name for name, field in IncidentPayload.model_fields.items()]
    rows = [
        translator.to_payload(item).model_dump(mode="json", by_alias=True, include=CSV_FIELDS)
        for item in incidents
    ]
    path.parent.mkdir(parents=True, exist_ok=True)
    pd.DataFrame.from_records(rows, columns=columns).to_csv(path, index=False)
