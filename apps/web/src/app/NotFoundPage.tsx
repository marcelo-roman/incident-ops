import { Link } from 'react-router';
import { PageHeader } from '../shared/ui/PageHeader';

export function NotFoundPage() {
  return (
    <PageHeader
      title="Page not found"
      summary={
        <>
          This address does not match a page in the console. <Link to="/">Go to the dashboard</Link>.
        </>
      }
    />
  );
}
