import IconCheck from '~icons/material-symbols/check'

export function PlayedIndicator() {
  return (
    <div
      className={`
        pointer-events-none absolute top-2 right-2 z-10 h-6 w-6 rounded-full
        bg-primary p-0.5
      `}
    >
      <IconCheck
        aria-hidden="true"
        className="absolute top-1 right-1 h-4 w-4 text-white/90"
      />
    </div>
  )
}
