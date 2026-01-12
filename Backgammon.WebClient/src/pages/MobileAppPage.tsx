import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export const MobileAppPage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <Card>
        <CardHeader>
          <CardTitle className="text-3xl">Mobile App</CardTitle>
        </CardHeader>
        <CardContent className="prose prose-invert max-w-none">
          <p className="text-muted-foreground text-lg">
            Play backgammon on the go with our mobile app.
          </p>
          <p className="text-muted-foreground mt-4">
            This page is under construction. Check back soon for download links and mobile app information.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
