import React from 'react'
import { NotFound } from '@/components/NotFound'

export const NotFoundPage: React.FC = () => {
  return (
    <NotFound
      title="404 - Page Not Found"
      message="The page you are looking for does not exist."
    />
  )
}
