import { Observable, catchError, map, of, startWith } from 'rxjs';
import { errorMessage } from './error-message';
export interface LoadState<T> {
  data: T | null;
  loading: boolean;
  error: string;
}
export function loadState<T>(source: Observable<T>): Observable<LoadState<T>> {
  return source.pipe(
    map((data) => ({ data, loading: false, error: '' })),
    startWith({ data: null, loading: true, error: '' } as LoadState<T>),
    catchError((error) => of({ data: null, loading: false, error: errorMessage(error) })),
  );
}
