import { useSearchParams } from 'react-router';
import { parseInsightWindow, type InsightWindow } from '../domain/insights';

export function useInsightWindow() {
  const [params, setParams] = useSearchParams();
  const days = parseInsightWindow(params.get('days'));
  const setDays = (next: InsightWindow) => {
    setParams({ days: String(next) }, { replace: true });
  };
  return { days, setDays };
}
