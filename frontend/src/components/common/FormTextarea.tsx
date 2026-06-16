import * as React from 'react'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'

interface FormTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string
  error?: string
}

export const FormTextarea = React.forwardRef<HTMLTextAreaElement, FormTextareaProps>(
  ({ label, error, className, id, ...props }, ref) => {
    const inputId = id ?? props.name
    return (
      <div className="space-y-1.5">
        {label && <Label htmlFor={inputId}>{label}</Label>}
        <Textarea id={inputId} ref={ref} className={cn(error && 'border-danger focus-visible:ring-danger', className)} {...props} />
        {error && <p className="text-xs text-danger">{error}</p>}
      </div>
    )
  },
)
FormTextarea.displayName = 'FormTextarea'
