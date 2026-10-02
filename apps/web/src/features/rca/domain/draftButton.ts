export interface DraftRequestState {
  isPending: boolean;
  hasDraft: boolean;
}

export function draftButtonLabel(state: DraftRequestState): string {
  if (state.isPending) {
    return 'Drafting RCA';
  }
  if (state.hasDraft) {
    return 'Draft again';
  }
  return 'Draft RCA with AI';
}
