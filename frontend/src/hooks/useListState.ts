import { useMemo, useState } from 'react'
import type { PagedQuery } from '@/types/api'

/** Shared list state for DataTable pages: page, search, and sort with a derived query. */
export function useListState(initial?: Partial<PagedQuery>) {
  const [page, setPage] = useState(initial?.page ?? 1)
  const pageSize = initial?.pageSize ?? 20
  const [search, setSearchRaw] = useState(initial?.search ?? '')
  const [sortBy, setSortBy] = useState<string | undefined>(initial?.sortBy)
  const [sortDescending, setSortDescending] = useState(initial?.sortDescending ?? false)

  function setSearch(value: string) {
    setSearchRaw(value)
    setPage(1)
  }

  function toggleSort(key: string) {
    if (sortBy === key) {
      setSortDescending((d) => !d)
    } else {
      setSortBy(key)
      setSortDescending(false)
    }
    setPage(1)
  }

  function clear() {
    setSearchRaw('')
    setSortBy(undefined)
    setSortDescending(false)
    setPage(1)
  }

  const query: PagedQuery = useMemo(
    () => ({ page, pageSize, search: search || undefined, sortBy, sortDescending }),
    [page, pageSize, search, sortBy, sortDescending],
  )

  return { page, setPage, pageSize, search, setSearch, sortBy, sortDescending, toggleSort, clear, query }
}
