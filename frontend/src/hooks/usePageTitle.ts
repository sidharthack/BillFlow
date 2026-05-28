import { useEffect } from 'react';

export function usePageTitle(title: string) {
  useEffect(() => {
    document.title = `${title} — BillFlow`;
    return () => { document.title = 'BillFlow'; };
  }, [title]);
}