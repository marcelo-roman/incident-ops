import { sessionHttpAuthentication } from '../features/auth';
import { setHttpAuthentication } from '../shared/http/authentication';

export function connectSessionToHttp(): void {
  setHttpAuthentication(sessionHttpAuthentication);
}
