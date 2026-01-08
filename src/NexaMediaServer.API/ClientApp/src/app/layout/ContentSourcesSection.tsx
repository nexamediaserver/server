import { useQuery } from '@apollo/client/react'
import { Link, useRouterState } from '@tanstack/react-router'
import { useAtom } from 'jotai'
import { Suspense, useEffect } from 'react'

import { librarySectionsQueryDocument } from '@/app/graphql/content-source'
import { AddContentSourceDialog } from '@/features/content-sources/components/AddContentSourceDialog'
import { ContentTypeIcon } from '@/features/content-sources/components/ContentTypeIcon'
import { LibraryActionsMenu } from '@/features/content-sources/components/LibraryActionsMenu'
import { libraryViewModesAtom } from '@/features/content-sources/store/atoms'
import {
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/shared/components/ui/sidebar'

export function ContentSourcesSection() {
  const routerState = useRouterState()
  const pathname = routerState.location.pathname

  const { data } = useQuery(librarySectionsQueryDocument, {
    variables: { first: 50 },
  })

  const [libraryViewModes, setLibraryViewModes] = useAtom(libraryViewModesAtom)

  // Clean up view mode preferences for deleted libraries
  useEffect(() => {
    const nodes = data?.librarySections?.nodes
    if (!nodes || nodes.length === 0) {
      return
    }

    const validIds = new Set(nodes.map((node) => node.id))
    const storedIds = Object.keys(libraryViewModes)
    const orphanedIds = storedIds.filter((id) => !validIds.has(id))

    if (orphanedIds.length > 0) {
      setLibraryViewModes((prev) => {
        const updated = { ...prev }
        for (const id of orphanedIds) {
          delete updated[id]
        }
        return updated
      })
    }
  }, [data?.librarySections?.nodes, libraryViewModes, setLibraryViewModes])

  return (
    <Suspense>
      <SidebarGroupLabel className="justify-between pr-0">
        Sections
        <AddContentSourceDialog />
      </SidebarGroupLabel>
      <SidebarMenu>
        {data?.librarySections?.nodes?.map((cs) => (
          <SidebarMenuItem
            className="group flex items-center justify-between"
            key={cs.id}
          >
            <div className="flex w-full items-center gap-2">
              <SidebarMenuButton
                asChild
                className="flex-1 justify-start"
                isActive={pathname.startsWith(
                  `/section/${encodeURIComponent(cs.id)}`,
                )}
                tooltip={cs.name}
              >
                <Link
                  params={{ contentSourceId: cs.id }}
                  to="/section/$contentSourceId"
                >
                  <ContentTypeIcon contentType={cs.type} />
                  {cs.name}
                </Link>
              </SidebarMenuButton>

              <LibraryActionsMenu librarySectionId={cs.id} />
            </div>
          </SidebarMenuItem>
        ))}
      </SidebarMenu>
    </Suspense>
  )
}
