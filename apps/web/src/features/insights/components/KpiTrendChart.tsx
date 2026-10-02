import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  type TooltipContentProps,
} from 'recharts';
import { formatShortDate } from '../../../shared/format/dates';
import styles from './Insights.module.css';
import type { TrendMetricDefinition, TrendPoint } from '../domain/trendMetrics';

interface KpiTrendChartProps {
  points: TrendPoint[];
  metric: TrendMetricDefinition;
}

function TrendTooltip({ active, payload, metric }: TooltipContentProps & { metric: TrendMetricDefinition }) {
  const point = payload[0]?.payload as TrendPoint | undefined;
  if (!active || point === undefined) {
    return null;
  }
  return (
    <div className={styles.tooltip}>
      <div>Week of {formatShortDate(point.weekStart)}</div>
      <div>
        {metric.label}: <span className={styles.tooltipValue}>{metric.format(point.value)}</span>
      </div>
    </div>
  );
}

export function KpiTrendChart({ points, metric }: KpiTrendChartProps) {
  return (
    <div className={styles.chart} role="img" aria-label={`${metric.label} per week`}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={points} margin={{ top: 8, right: 12, bottom: 0, left: 4 }}>
          <CartesianGrid vertical={false} stroke="var(--color-chart-grid)" />
          <XAxis
            dataKey="weekStart"
            tickFormatter={formatShortDate}
            stroke="var(--color-text-muted)"
            tickLine={false}
            axisLine={{ stroke: 'var(--color-line-strong)' }}
            minTickGap={24}
          />
          <YAxis
            tickFormatter={(value: number) => metric.format(value)}
            stroke="var(--color-text-muted)"
            tickLine={false}
            axisLine={false}
            width={56}
          />
          <Tooltip
            cursor={{ stroke: 'var(--color-line-strong)' }}
            content={(props) => <TrendTooltip {...props} metric={metric} />}
          />
          <Line
            type="monotone"
            dataKey="value"
            stroke="var(--color-chart-1)"
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 4, stroke: 'var(--color-surface)', strokeWidth: 2 }}
            connectNulls
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
