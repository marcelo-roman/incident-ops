from incident_insights.application.ports import RcaDrafter
from incident_insights.domain.rca.rca_draft_composer import RcaDraftComposer
from incident_insights.infrastructure.incidents.acl import IncidentTranslator
from incident_insights.infrastructure.rca.azure_openai_drafter import (
    AzureOpenAIRcaDrafter,
    create_azure_openai_client,
)
from incident_insights.infrastructure.rca.fallback_drafter import DeterministicRcaDrafter
from incident_insights.infrastructure.rca.prompt import RcaPromptBuilder
from incident_insights.infrastructure.settings import Settings


def build_rca_drafter(settings: Settings) -> RcaDrafter:
    if not settings.azure_openai_endpoint or not settings.azure_openai_deployment:
        return DeterministicRcaDrafter(RcaDraftComposer())
    client = create_azure_openai_client(
        settings.azure_openai_endpoint, settings.azure_openai_api_version
    )
    prompt = RcaPromptBuilder(IncidentTranslator())
    return AzureOpenAIRcaDrafter(client, settings.azure_openai_deployment, prompt)
