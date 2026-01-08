import type { ReactNode } from 'react'

import IconNotifications from '~icons/material-symbols/notifications'
import IconNotificationsActive from '~icons/material-symbols/notifications-active'

import type { OnJobNotificationSubscription } from '@/shared/api/graphql/graphql'

import { useJobNotifications } from '@/app/providers'
import { JobType } from '@/shared/api/graphql/graphql'
import { Badge } from '@/shared/components/ui/badge'
import { Button } from '@/shared/components/ui/button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/shared/components/ui/popover'
import { Progress } from '@/shared/components/ui/progress'

type JobNotification = OnJobNotificationSubscription['onJobNotification']

export function NotificationButton() {
  const { activeJobs, hasActiveJobs } = useJobNotifications()
  const activeJobsArray = Array.from(activeJobs.values())

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button className="relative" size="icon" variant="ghost">
          {hasActiveJobs ? (
            <>
              <IconNotificationsActive />
              <Badge
                className={`
                  absolute -top-1 -right-1 h-5 w-5 items-center justify-center
                  rounded-full p-0 text-xs
                `}
                variant="default"
              >
                {activeJobsArray.length}
              </Badge>
            </>
          ) : (
            <IconNotifications />
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-96">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h4 className="text-sm font-semibold">Activity</h4>
            {hasActiveJobs && (
              <Badge variant="secondary">{activeJobsArray.length}</Badge>
            )}
          </div>
          <div className="max-h-100 space-y-4 overflow-y-auto">
            {!hasActiveJobs ? (
              <div className="py-8 text-center text-sm text-muted-foreground">
                No activity
              </div>
            ) : (
              activeJobsArray.map((job) => (
                <JobNotificationItem job={job} key={job.id} />
              ))
            )}
          </div>
        </div>
      </PopoverContent>
    </Popover>
  )
}

function getJobTypeLabel(jobType: JobType): ReactNode {
  switch (jobType) {
    case JobType.FileAnalysis:
      return 'Analysis'
    case JobType.ImageGeneration:
      return 'Images'
    case JobType.LibraryScan:
      return 'Scan'
    case JobType.MetadataRefresh:
      return 'Metadata'
    case JobType.TrickplayGeneration:
      return 'Trickplay'
    default:
      return jobType
  }
}

function JobNotificationItem({ job }: { job: JobNotification }) {
  const progressText =
    job.totalItems > 0
      ? `${String(job.completedItems)} / ${String(job.totalItems)}`
      : `${String(Math.round(job.progressPercentage))}%`

  return (
    <div className="space-y-2 rounded-lg border p-3">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="text-sm font-medium">{job.description}</div>
          {job.librarySectionName && (
            <div className="text-xs text-muted-foreground">
              {job.librarySectionName}
            </div>
          )}
        </div>
        <Badge className="ml-2" variant="outline">
          {getJobTypeLabel(job.type)}
        </Badge>
      </div>
      <div className="space-y-1">
        <Progress value={job.progressPercentage} />
        <div className="text-right text-xs text-muted-foreground">
          {progressText}
        </div>
      </div>
    </div>
  )
}
