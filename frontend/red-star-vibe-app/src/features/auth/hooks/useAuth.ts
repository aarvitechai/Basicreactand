import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { authApi } from '../api/authApi'
import type { User } from '../types'

export const authKeys = {
  me: ['auth', 'me'] as const,
}

/** The signed-in user (`null` when signed out, `undefined` while the session is being checked). */
export function useCurrentUser() {
  return useQuery({
    queryKey: authKeys.me,
    queryFn: ({ signal }) => authApi.me(signal),
    staleTime: 5 * 60_000,
  })
}

export function useRegister() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: authApi.register,
    onSuccess: (user) => queryClient.setQueryData<User | null>(authKeys.me, user),
  })
}

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: authApi.login,
    onSuccess: (user) => queryClient.setQueryData<User | null>(authKeys.me, user),
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: authApi.logout,
    onSettled: () => {
      // Drop any cached data that belonged to the previous user.
      queryClient.clear()
      queryClient.setQueryData<User | null>(authKeys.me, null)
    },
  })
}
