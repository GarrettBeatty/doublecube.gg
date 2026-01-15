import { cn } from '@/lib/utils'
import { Label } from './label'

interface ColorPickerProps {
  value: string
  onChange: (value: string) => void
  label?: string
  className?: string
}

export function ColorPicker({
  value,
  onChange,
  label,
  className,
}: ColorPickerProps) {
  return (
    <div className={cn('flex items-center gap-3', className)}>
      {label && (
        <Label className="text-sm text-muted-foreground min-w-[120px]">
          {label}
        </Label>
      )}
      <div className="flex items-center gap-2">
        <input
          type="color"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="h-8 w-12 cursor-pointer rounded border border-input bg-transparent p-0.5"
        />
        <span className="text-xs font-mono text-muted-foreground w-[70px]">
          {value.toUpperCase()}
        </span>
      </div>
    </div>
  )
}
