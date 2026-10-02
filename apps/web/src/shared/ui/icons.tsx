import type { SVGProps } from 'react';

type IconProps = SVGProps<SVGSVGElement>;

function Icon({ children, ...props }: Readonly<IconProps>) {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
      {...props}
    >
      {children}
    </svg>
  );
}

export function BoardIcon(props: Readonly<IconProps>) {
  return (
    <Icon {...props}>
      <rect x="2" y="2.5" width="5" height="11" rx="1" />
      <rect x="9" y="2.5" width="5" height="6" rx="1" />
      <path d="M9 11.5h5" />
    </Icon>
  );
}

export function ListIcon(props: Readonly<IconProps>) {
  return (
    <Icon {...props}>
      <path d="M5.5 4h8M5.5 8h8M5.5 12h8" />
      <path d="M2.5 4h.01M2.5 8h.01M2.5 12h.01" />
    </Icon>
  );
}

export function SignalIcon(props: Readonly<IconProps>) {
  return (
    <Icon {...props}>
      <path d="M8 2 1.75 13.5h12.5z" />
      <path d="M8 6.5v3" />
      <path d="M8 11.75v.01" />
    </Icon>
  );
}

export function TrendIcon(props: Readonly<IconProps>) {
  return (
    <Icon {...props}>
      <path d="M2 12.5l4-4 2.5 2.5L14 5" />
      <path d="M10.5 5H14v3.5" />
    </Icon>
  );
}
