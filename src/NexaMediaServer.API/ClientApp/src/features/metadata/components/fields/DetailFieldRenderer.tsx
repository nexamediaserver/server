import type { ReactNode } from 'react'

import { Link } from '@tanstack/react-router'
import { Duration } from 'luxon'
import { useState } from 'react'

import type { MediaQuery } from '@/shared/api/graphql/graphql'

import {
  DetailFieldType,
  DetailFieldWidgetType,
} from '@/shared/api/graphql/graphql'

interface DetailFieldRendererProps {
  contentSourceId: string
  customFieldKey?: null | string
  extraFields: ExtraField[]
  fieldType: DetailFieldType
  label: string
  metadataItem: MetadataItem
  widget: DetailFieldWidgetType
}
type ExtraField = MetadataItem['extraFields'][number]

type MetadataItem = NonNullable<MediaQuery['metadataItem']>

const formatRuntime = (lengthSeconds?: null | number): null | string => {
  if (!lengthSeconds || lengthSeconds <= 0) {
    return null
  }

  const locale = Intl.DateTimeFormat().resolvedOptions().locale
  const duration = Duration.fromObject({ seconds: lengthSeconds })
    .shiftTo('hours', 'minutes')
    .normalize()

  const hours = Math.trunc(duration.hours)
  const roundedMinutes = Math.round(duration.minutes)

  const totalHours = hours + Math.floor(roundedMinutes / 60)
  const minutes = roundedMinutes % 60

  const hourFormatter = new Intl.NumberFormat(locale, {
    maximumFractionDigits: 0,
    style: 'unit',
    unit: 'hour',
    unitDisplay: 'narrow',
  })
  const minuteFormatter = new Intl.NumberFormat(locale, {
    maximumFractionDigits: 0,
    style: 'unit',
    unit: 'minute',
    unitDisplay: 'narrow',
  })

  const parts: string[] = []
  if (totalHours > 0) {
    parts.push(hourFormatter.format(totalHours))
  }

  const displayMinutes =
    minutes > 0 || parts.length === 0 ? Math.max(1, minutes) : 0
  if (displayMinutes > 0) {
    parts.push(minuteFormatter.format(displayMinutes))
  }

  return parts.join(' ')
}

interface ExpandableListProps {
  contentSourceId: string
  items: string[]
  linkParamKey?: string
  maxItems?: number
}

export function DetailFieldRenderer({
  contentSourceId,
  customFieldKey,
  extraFields,
  fieldType,
  label,
  metadataItem,
  widget,
}: DetailFieldRendererProps): ReactNode {
  const value = getFieldValue(
    fieldType,
    metadataItem,
    extraFields,
    customFieldKey,
  )

  // Skip rendering if value is empty/null
  if (value === null || value === undefined) {
    return null
  }

  // For arrays, skip if empty
  if (Array.isArray(value) && value.length === 0) {
    return null
  }

  // For year, validate it's a positive integer
  if (fieldType === DetailFieldType.Year) {
    const year = value as number
    if (!Number.isInteger(year) || year <= 0) {
      return null
    }
  }

  switch (widget) {
    case DetailFieldWidgetType.Badge:
      return (
        <span
          className={`
            rounded border border-white/40 px-1.5 py-0.5 text-xs
            text-muted-foreground
          `}
        >
          {String(value)}
        </span>
      )

    case DetailFieldWidgetType.Boolean:
      return (
        <span className="text-sm">
          {label}: {value ? 'Yes' : 'No'}
        </span>
      )

    case DetailFieldWidgetType.Date:
      if (typeof value === 'string') {
        const date = new Date(value)
        if (!isNaN(date.getTime())) {
          return (
            <span className="text-sm text-muted-foreground">
              {date.toLocaleDateString()}
            </span>
          )
        }
      }
      return (
        <span className="text-sm text-muted-foreground">{String(value)}</span>
      )

    case DetailFieldWidgetType.Duration: {
      const runtime = formatRuntime(value as number)
      if (!runtime) return null
      return <span className="text-sm text-muted-foreground">{runtime}</span>
    }

    case DetailFieldWidgetType.Heading:
      return (
        <h1 className="line-clamp-1 text-4xl font-bold">{String(value)}</h1>
      )

    case DetailFieldWidgetType.Link:
      return (
        <a
          className={`
            text-sm text-primary
            hover:underline
          `}
          href={String(value)}
          rel="noopener noreferrer"
          target="_blank"
        >
          {label}
        </a>
      )

    case DetailFieldWidgetType.List: {
      const items = value as string[]
      const linkParam =
        fieldType === DetailFieldType.Genres ? 'genre' : undefined
      return (
        <ExpandableList
          contentSourceId={contentSourceId}
          items={items}
          linkParamKey={linkParam}
        />
      )
    }

    case DetailFieldWidgetType.Number:
      return (
        <span className="text-sm text-muted-foreground">
          {typeof value === 'number' ? value.toLocaleString() : String(value)}
        </span>
      )

    case DetailFieldWidgetType.Text:
      if (fieldType === DetailFieldType.OriginalTitle) {
        return (
          <h2 className="line-clamp-1 text-lg font-light">{String(value)}</h2>
        )
      }
      if (fieldType === DetailFieldType.Year) {
        return (
          <span className="text-sm text-muted-foreground">{String(value)}</span>
        )
      }
      return <span className="text-sm">{String(value)}</span>

    default:
      return <span className="text-sm">{String(value)}</span>
  }
}

function ExpandableList({
  contentSourceId,
  items,
  linkParamKey,
  maxItems = 3,
}: ExpandableListProps): ReactNode {
  const [showAll, setShowAll] = useState(false)

  if (!items || items.length === 0) {
    return null
  }

  const displayItems =
    showAll || items.length <= maxItems ? items : items.slice(0, maxItems - 1)

  return (
    <div className="flex flex-row flex-wrap items-baseline">
      {displayItems.map((item, index, array) => (
        <span key={item}>
          {linkParamKey ? (
            <Link params={{ contentSourceId, [linkParamKey]: item }}>
              {item}
            </Link>
          ) : (
            item
          )}
          {index < array.length - 1 && ',\u00A0'}
        </span>
      ))}
      {items.length > maxItems && !showAll && (
        <>
          <span>,&nbsp;</span>
          <button
            className="hover:underline"
            onClick={() => {
              setShowAll(true)
            }}
            type="button"
          >
            and more
          </button>
        </>
      )}
    </div>
  )
}

function getFieldValue(
  fieldType: DetailFieldType,
  metadataItem: MetadataItem,
  extraFields: ExtraField[],
  customFieldKey?: null | string,
): unknown {
  switch (fieldType) {
    case DetailFieldType.ContentRating:
      return metadataItem.contentRating
    case DetailFieldType.Custom:
      if (customFieldKey) {
        const field = extraFields.find((f) => f.key === customFieldKey)
        return field?.value
      }
      return null
    case DetailFieldType.Genres:
      return metadataItem.genres
    case DetailFieldType.OriginalTitle:
      return metadataItem.originalTitle
    case DetailFieldType.Runtime:
      return metadataItem.length
    case DetailFieldType.Tags:
      return metadataItem.tags
    case DetailFieldType.Title:
      return metadataItem.title
    case DetailFieldType.Year:
      return metadataItem.year
    default:
      return null
  }
}
