from pathlib import Path
from threading import Lock
from uuid import UUID

import pandas as pd
from pydantic import TypeAdapter, ValidationError

from incident_insights.application.ports import IncidentSourceError
from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.infrastructure.incidents.acl import IncidentPayload, IncidentTranslator

_ROWS = TypeAdapter(list[IncidentPayload])


class CsvIncidentSource:
    def __init__(self, path: Path, translator: IncidentTranslator) -> None:
        self._path = path
        self._translator = translator
        self._history: IncidentHistory | None = None
        self._lock = Lock()

    def export(self, window: ReportingWindow) -> IncidentHistory:
        return self._load().within(window)

    def get(self, incident_id: UUID) -> IncidentRecord | None:
        return self._load().find(incident_id)

    def _load(self) -> IncidentHistory:
        with self._lock:
            if self._history is None:
                self._history = self._read()
            return self._history

    def _read(self) -> IncidentHistory:
        if not self._path.is_file():
            raise IncidentSourceError(f"Incident CSV not found: {self._path.name}")
        frame = pd.read_csv(self._path, dtype=str, keep_default_na=False)
        rows = [
            {key: value for key, value in row.items() if value != ""}
            for row in frame.to_dict(orient="records")
        ]
        try:
            return IncidentHistory(
                self._translator.to_record(item) for item in _ROWS.validate_python(rows)
            )
        except (ValidationError, ValueError) as error:
            raise IncidentSourceError(f"Incident CSV is invalid: {self._path.name}") from error
