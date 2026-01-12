import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export const AboutPage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <Card>
        <CardHeader>
          <CardTitle className="text-3xl">About</CardTitle>
        </CardHeader>
        <CardContent className="prose prose-invert max-w-none">
          <p className="text-muted-foreground text-lg">
            Welcome to Backgammon - a free online platform to play backgammon against friends, AI opponents, or players from around the world.
          </p>
          <p className="text-muted-foreground mt-4">
            This page is under construction. Check back soon for more information about our platform, team, and mission.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
