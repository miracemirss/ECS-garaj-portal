import type { ReactNode } from 'react'
import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { EmptyState } from './EmptyState'

export interface Column<T> {
  key: string
  header: string
  cell: (row: T) => ReactNode
  sortable?: boolean
  className?: string
}

interface DataTableProps<T> {
  columns: Column<T>[]
  data: T[]
  rowKey: (row: T) => string
  loading?: boolean
  emptyMessage?: string
  onRowClick?: (row: T) => void
  page?: number
  pageSize?: number
  totalCount?: number
  totalPages?: number
  onPageChange?: (page: number) => void
  sortBy?: string
  sortDescending?: boolean
  onSortChange?: (key: string) => void
}

export function DataTable<T>({
  columns,
  data,
  rowKey,
  loading,
  emptyMessage,
  onRowClick,
  page,
  totalCount,
  totalPages,
  onPageChange,
  sortBy,
  sortDescending,
  onSortChange,
}: DataTableProps<T>) {
  const showPagination = page != null && totalPages != null && onPageChange != null

  return (
    <div className="overflow-hidden rounded-lg border bg-card">
      <Table>
        <TableHeader>
          <TableRow>
            {columns.map((col) => (
              <TableHead key={col.key} className={col.className}>
                {col.sortable && onSortChange ? (
                  <button type="button" className="inline-flex items-center gap-1 hover:text-foreground" onClick={() => onSortChange(col.key)}>
                    {col.header}
                    {sortBy === col.key ? (
                      sortDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />
                    ) : (
                      <ChevronsUpDown className="h-3.5 w-3.5 opacity-50" />
                    )}
                  </button>
                ) : (
                  col.header
                )}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {loading ? (
            Array.from({ length: 6 }).map((_, i) => (
              <TableRow key={i}>
                {columns.map((col) => (
                  <TableCell key={col.key}>
                    <Skeleton className="h-4 w-24" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : data.length === 0 ? (
            <TableRow>
              <TableCell colSpan={columns.length}>
                <EmptyState description={emptyMessage} />
              </TableCell>
            </TableRow>
          ) : (
            data.map((row) => (
              <TableRow
                key={rowKey(row)}
                className={cn(onRowClick && 'cursor-pointer')}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
              >
                {columns.map((col) => (
                  <TableCell key={col.key} className={col.className}>
                    {col.cell(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>

      {showPagination && (
        <div className="flex flex-col gap-2 border-t px-3 py-2 text-sm text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
          <span>{totalCount != null ? `${totalCount} kayıt` : ''}</span>
          <div className="flex flex-wrap items-center gap-2">
            <span>
              Sayfa {page} / {Math.max(totalPages ?? 1, 1)}
            </span>
            <Button variant="outline" size="sm" disabled={(page ?? 1) <= 1} onClick={() => onPageChange?.((page ?? 1) - 1)}>
              Önceki
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={(page ?? 1) >= (totalPages ?? 1)}
              onClick={() => onPageChange?.((page ?? 1) + 1)}
            >
              Sonraki
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
