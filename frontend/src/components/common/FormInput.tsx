import * as React from 'react'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'

interface FormInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export const FormInput = React.forwardRef<HTMLInputElement, FormInputProps>(
  ({ label, error, className, id, ...props }, ref) => {
    const inputId = id ?? props.name
    return (
      <div className="space-y-1.5">
        {label && <Label htmlFor={inputId}>{label}</Label>}
        <Input id={inputId} ref={ref} className={cn(error && 'border-danger focus-visible:ring-danger', className)} {...props} />
        {error && <p className="text-xs text-danger">{error}</p>}
      </div>
    )
  },
)
FormInput.displayName = 'FormInput'
