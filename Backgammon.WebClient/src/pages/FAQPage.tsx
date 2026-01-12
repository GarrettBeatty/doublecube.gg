import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export const FAQPage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <Card>
        <CardHeader>
          <CardTitle className="text-3xl">Frequently Asked Questions</CardTitle>
        </CardHeader>
        <CardContent className="prose prose-invert max-w-none">
          <p className="text-muted-foreground text-lg">
            Find answers to common questions about playing backgammon on our platform.
          </p>
          <p className="text-muted-foreground mt-4">
            This page is under construction. Check back soon for a comprehensive FAQ section.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
