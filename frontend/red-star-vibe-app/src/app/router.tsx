import { createBrowserRouter } from 'react-router'
import { HomePage } from '@/pages/HomePage'
import { Layout } from './Layout'
import { RouteError } from './RouteError'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <HomePage /> },
      // Lazy-load larger pages to keep the initial bundle small, e.g.:
      // { path: 'settings', lazy: () => import('@/pages/SettingsPage').then((m) => ({ Component: m.SettingsPage })) },
    ],
  },
])
