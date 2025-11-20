import { useQuery } from '@apollo/client/react'
import { useForm } from '@tanstack/react-form'
import { useRouter } from '@tanstack/react-router'
import { useIsFirstRender } from '@uidotdev/usehooks'
import { useEffect, useMemo, useState } from 'react'

import { useAuth } from '@/features/auth'
import { graphql } from '@/shared/api/graphql'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'

const serverInfo = graphql(`
  query ServerInfo {
    serverInfo {
      versionString
      isDevelopment
    }
  }
`)

export const LoginPage = () => {
  const { data } = useQuery(serverInfo)
  const { isAuthenticated, login, status } = useAuth()
  const router = useRouter()
  const [error, setError] = useState<null | string>(null)
  const isFirstRender = useIsFirstRender()

  // Compute safe redirect target once per render
  const next = useMemo(() => {
    const params = new URLSearchParams(globalThis.location.search)
    const nextParam = params.get('next')
    const isSafe = (v: null | string) =>
      !!v && v.startsWith('/') && !v.startsWith('/login')
    return isSafe(nextParam) ? nextParam : '/'
  }, [])

  // If already authenticated, redirect away from login
  // Primary redirect when auth state flips (skip on first render to avoid flash)
  useEffect(() => {
    if (!isAuthenticated || isFirstRender) return
    void router.navigate({ replace: true, to: next ?? '/' })
  }, [isAuthenticated, isFirstRender, router, next])

  // Fallback: if still on /login shortly after authentication, force hard navigation.
  useEffect(() => {
    if (!isAuthenticated) return
    const pathname = globalThis.location.pathname
    if (pathname !== '/login') return
    const id = setTimeout(() => {
      if (globalThis.location.pathname === '/login') {
        // Try router again if first attempt didn't move us
        void router.navigate({ replace: true, to: next ?? '/' }).catch(() => {
          // Last resort: full page reload to root
          globalThis.location.replace(next ?? '/')
        })
      }
    }, 150) // short delay to allow SPA state updates
    return () => {
      clearTimeout(id)
    }
  }, [isAuthenticated, next, router])

  const form = useForm({
    defaultValues: { password: '', username: '' },
    onSubmit: async ({ value }) => {
      setError(null)
      try {
        await login(value.username.trim(), value.password, {
          clientVersion: data?.serverInfo.versionString ?? null,
        })
        const params = new URLSearchParams(globalThis.location.search)
        const nextParam = params.get('next')
        const target =
          nextParam &&
          nextParam.startsWith('/') &&
          !nextParam.startsWith('/login')
            ? nextParam
            : '/'
        void router.navigate({ replace: true, to: target })
      } catch (err) {
        setError((err as Error).message || 'Login failed')
      }
    },
  })

  const submitting = form.state.isSubmitting || status === 'loading'

  return (
    <main
      className={`
        flex min-h-screen flex-col items-center justify-center gap-2 p-4
      `}
      id="maincontent"
    >
      <h1 className="px-1 pb-1 text-7xl font-bold select-none">
        ne<span className="font-black text-purple-500">x</span>a
      </h1>
      {!isAuthenticated && (
        <form
          aria-describedby={error ? 'login-error' : undefined}
          className={`
            w-full max-w-sm space-y-5 rounded-md border bg-white p-6 shadow-sm
            dark:border-stone-700 dark:bg-stone-800
          `}
          noValidate
          onSubmit={(e) => {
            e.preventDefault()
            void form.handleSubmit()
          }}
        >
          <h1 className="text-xl font-semibold">Sign in</h1>
          {error && (
            <p
              className="rounded bg-red-100 px-2 py-1 text-sm text-red-700"
              id="login-error"
              role="alert"
            >
              {error}
            </p>
          )}
          <form.Field
            children={(field) => (
              <div className="space-y-1">
                <Label htmlFor={field.name}>Username</Label>
                <Input
                  aria-describedby={
                    field.state.meta.errors.length
                      ? 'username-error'
                      : undefined
                  }
                  aria-invalid={
                    field.state.meta.errors.length ? 'true' : undefined
                  }
                  autoComplete="username"
                  id={field.name}
                  onBlur={field.handleBlur}
                  onChange={(e) => {
                    field.handleChange(e.target.value)
                  }}
                  value={field.state.value}
                />
                {field.state.meta.errors[0] && (
                  <p
                    className="text-xs text-red-600"
                    id="username-error"
                    role="alert"
                  >
                    {field.state.meta.errors[0]}
                  </p>
                )}
              </div>
            )}
            name="username"
            validators={{
              onChange: ({ value }: { value: string }) =>
                value ? undefined : 'Username is required',
            }}
          />
          <form.Field
            children={(field) => (
              <div className="space-y-1">
                <Label htmlFor={field.name}>Password</Label>
                <Input
                  aria-describedby={
                    field.state.meta.errors.length
                      ? 'password-error'
                      : undefined
                  }
                  aria-invalid={
                    field.state.meta.errors.length ? 'true' : undefined
                  }
                  autoComplete="current-password"
                  id={field.name}
                  onBlur={field.handleBlur}
                  onChange={(e) => {
                    field.handleChange(e.target.value)
                  }}
                  type="password"
                  value={field.state.value}
                />
                {field.state.meta.errors[0] && (
                  <p
                    className="text-xs text-red-600"
                    id="password-error"
                    role="alert"
                  >
                    {field.state.meta.errors[0]}
                  </p>
                )}
              </div>
            )}
            name="password"
            validators={{
              onChange: ({ value }: { value: string }) =>
                value ? undefined : 'Password is required',
            }}
          />
          <Button
            className="w-full"
            disabled={submitting || !form.state.canSubmit}
            type="submit"
          >
            {submitting ? 'Signing in…' : 'Sign in'}
          </Button>
        </form>
      )}
      <div className="fixed bottom-2 text-center text-xs text-stone-300">
        {data?.serverInfo.isDevelopment && (
          <p>Development build - {data.serverInfo.versionString}</p>
        )}
        {!data?.serverInfo.isDevelopment && (
          <p>Version {data?.serverInfo.versionString}</p>
        )}
      </div>
    </main>
  )
}

export default LoginPage
