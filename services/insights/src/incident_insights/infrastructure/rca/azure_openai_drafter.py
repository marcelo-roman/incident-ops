from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from openai import AzureOpenAI, OpenAIError
from pydantic import ValidationError

from incident_insights.application.ports import RcaDraftError
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.rca.draft import InvalidRcaDraftError, RcaDraft
from incident_insights.infrastructure.rca.prompt import RcaPromptBuilder
from incident_insights.infrastructure.rca.schema import RcaDraftSchema

COGNITIVE_SERVICES_SCOPE = "https://cognitiveservices.azure.com/.default"
MAX_COMPLETION_TOKENS = 8000


class AzureOpenAIRcaDrafter:
    def __init__(self, client: AzureOpenAI, deployment: str, prompt: RcaPromptBuilder) -> None:
        self._client = client
        self._deployment = deployment
        self._prompt = prompt

    @property
    def name(self) -> str:
        return f"azure-openai:{self._deployment}"

    def draft(self, incident: IncidentRecord) -> RcaDraft:
        try:
            completion = self._client.chat.completions.parse(
                model=self._deployment,
                messages=self._prompt.messages(incident),
                response_format=RcaDraftSchema,
                max_completion_tokens=MAX_COMPLETION_TOKENS,
            )
        except (OpenAIError, ValidationError) as error:
            raise RcaDraftError(f"Azure OpenAI request failed: {error}") from error
        message = completion.choices[0].message
        if message.parsed is None:
            raise RcaDraftError(message.refusal or "Azure OpenAI returned no structured draft")
        try:
            return message.parsed.to_domain()
        except InvalidRcaDraftError as error:
            raise RcaDraftError(f"Azure OpenAI returned an invalid draft: {error}") from error


def create_azure_openai_client(endpoint: str, api_version: str) -> AzureOpenAI:
    token_provider = get_bearer_token_provider(DefaultAzureCredential(), COGNITIVE_SERVICES_SCOPE)
    return AzureOpenAI(
        azure_endpoint=endpoint,
        api_version=api_version,
        azure_ad_token_provider=token_provider,
    )
