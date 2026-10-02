import json
from collections.abc import Callable
from typing import Any

import httpx2
import pytest
from openai import AzureOpenAI

from incident_insights.application.ports import RcaDraftError
from incident_insights.domain.rca.draft import ActionItem, Priority, RcaDraft, RcaTimelineEvent
from incident_insights.domain.rca.rca_draft_composer import RcaDraftComposer
from incident_insights.domain.shared.severity import Severity
from incident_insights.infrastructure.incidents.acl import IncidentTranslator
from incident_insights.infrastructure.rca.azure_openai_drafter import (
    MAX_COMPLETION_TOKENS,
    AzureOpenAIRcaDrafter,
)
from incident_insights.infrastructure.rca.factory import build_rca_drafter
from incident_insights.infrastructure.rca.fallback_drafter import DeterministicRcaDrafter
from incident_insights.infrastructure.rca.prompt import RcaPromptBuilder
from incident_insights.infrastructure.rca.schema import RcaDraftSchema
from incident_insights.infrastructure.settings import Settings
from tests.builders import make_record

DEPLOYMENT = "rca-drafts"

PROMPT = RcaPromptBuilder(IncidentTranslator())

DRAFT = RcaDraft(
    summary="Checkout latency",
    impact="Slow orders",
    timeline=(RcaTimelineEvent(at="2026-09-25T00:00:00Z", event="Triggered"),),
    contributing_factors=("Index dropped",),
    action_items=(ActionItem("Restore index", "checkout owners", Priority.P1),),
)

DRAFT_JSON = json.dumps(
    {
        "summary": "Checkout latency",
        "impact": "Slow orders",
        "timeline": [{"at": "2026-09-25T00:00:00Z", "event": "Triggered"}],
        "contributingFactors": ["Index dropped"],
        "actionItems": [{"title": "Restore index", "owner": "checkout owners", "priority": "P1"}],
    }
)


def _completion(
    content: str | None, refusal: str | None = None, finish_reason: str = "stop"
) -> dict[str, Any]:
    message = {"role": "assistant", "content": content, "refusal": refusal}
    return {
        "id": "chatcmpl-test",
        "object": "chat.completion",
        "created": 0,
        "model": "gpt-5.4-mini",
        "choices": [{"index": 0, "finish_reason": finish_reason, "message": message}],
    }


def _azure(
    handler: Callable[[httpx2.Request], httpx2.Response],
) -> tuple[AzureOpenAIRcaDrafter, list[httpx2.Request]]:
    requests: list[httpx2.Request] = []

    def record(request: httpx2.Request) -> httpx2.Response:
        requests.append(request)
        return handler(request)

    client = AzureOpenAI(
        api_key="test-key",
        azure_endpoint="https://example.openai.azure.com",
        api_version="2025-04-01-preview",
        http_client=httpx2.Client(transport=httpx2.MockTransport(record)),
        max_retries=0,
    )
    return AzureOpenAIRcaDrafter(client, DEPLOYMENT, PROMPT), requests


def test_azure_drafter_requests_structured_output_for_reasoning_models() -> None:
    drafter, requests = _azure(lambda request: httpx2.Response(200, json=_completion(DRAFT_JSON)))

    draft = drafter.draft(make_record())

    body = json.loads(requests[0].content)
    assert draft == DRAFT
    assert drafter.name == "azure-openai:rca-drafts"
    assert "/openai/deployments/rca-drafts/chat/completions" in str(requests[0].url)
    assert requests[0].url.params["api-version"] == "2025-04-01-preview"
    assert body["max_completion_tokens"] == MAX_COMPLETION_TOKENS
    assert "temperature" not in body
    assert "top_p" not in body
    assert body["response_format"]["type"] == "json_schema"
    assert body["response_format"]["json_schema"]["strict"] is True
    assert "contributingFactors" in body["response_format"]["json_schema"]["schema"]["properties"]


def test_azure_drafter_raises_on_refusal() -> None:
    completion = _completion(None, refusal="cannot help")
    drafter, _ = _azure(lambda request: httpx2.Response(200, json=completion))

    with pytest.raises(RcaDraftError, match="cannot help"):
        drafter.draft(make_record())


def test_azure_drafter_raises_when_output_is_truncated() -> None:
    completion = _completion('{"summary": "cut', finish_reason="length")
    drafter, _ = _azure(lambda request: httpx2.Response(200, json=completion))

    with pytest.raises(RcaDraftError, match="Azure OpenAI request failed"):
        drafter.draft(make_record())


def test_azure_drafter_wraps_http_errors() -> None:
    drafter, _ = _azure(lambda request: httpx2.Response(500, json={"error": {"message": "boom"}}))

    with pytest.raises(RcaDraftError, match="Azure OpenAI request failed"):
        drafter.draft(make_record())


def test_prompt_carries_incident_and_sla_targets_as_data() -> None:
    messages = PROMPT.messages(make_record(severity=Severity.SEV1))

    assert messages[0]["role"] == "system"
    payload = str(messages[1]["content"])
    assert '"serviceId": "checkout"' in payload
    assert '"resolveWithin": "4h"' in payload


def test_fallback_drafter_is_labelled() -> None:
    drafter = DeterministicRcaDrafter(RcaDraftComposer())

    assert drafter.name == "deterministic-fallback"
    assert drafter.draft(make_record()).timeline


def test_factory_uses_azure_only_when_endpoint_is_configured() -> None:
    configured = Settings(azure_openai_endpoint="https://example.openai.azure.com")

    assert isinstance(build_rca_drafter(Settings()), DeterministicRcaDrafter)
    assert isinstance(build_rca_drafter(configured), AzureOpenAIRcaDrafter)
    assert isinstance(
        build_rca_drafter(Settings(azure_openai_endpoint="")), DeterministicRcaDrafter
    )


def test_azure_drafter_rejects_drafts_that_break_domain_invariants() -> None:
    blank = json.dumps({**json.loads(DRAFT_JSON), "summary": " "})
    drafter, _ = _azure(lambda request: httpx2.Response(200, json=_completion(blank)))

    with pytest.raises(RcaDraftError, match="invalid draft"):
        drafter.draft(make_record())


def test_schema_maps_to_the_domain_draft() -> None:
    assert RcaDraftSchema.model_validate_json(DRAFT_JSON).to_domain() == DRAFT
