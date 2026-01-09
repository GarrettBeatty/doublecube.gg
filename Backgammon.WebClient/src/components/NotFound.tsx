import React from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent } from '@/components/ui/card'
import { AlertCircle } from 'lucide-react'

interface NotFoundProps {
  title?: string
  message?: string
  showHomeButton?: boolean
}

export const NotFound: React.FC<NotFoundProps> = ({
  title = 'Not Found',
  message = 'The resource you are looking for could not be found.',
  showHomeButton = true,
}) => {
  const navigate = useNavigate()

  return (
    <div className="min-h-screen bg-background flex items-center justify-center">
      <Card className="max-w-md">
        <CardContent className="p-8">
          <div className="text-center">
            <AlertCircle className="mx-auto h-12 w-12 text-destructive mb-4" />
            <div className="text-2xl font-semibold mb-2 text-destructive">{title}</div>
            <div className="text-sm text-muted-foreground mb-6">{message}</div>
            {showHomeButton && (
              <button
                onClick={() => navigate('/')}
                className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors"
              >
                Return to Home
              </button>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
