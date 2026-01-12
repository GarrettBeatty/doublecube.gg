import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export const TermsOfServicePage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <Card>
        <CardHeader>
          <CardTitle className="text-3xl">Terms of Service</CardTitle>
        </CardHeader>
        <CardContent className="prose prose-invert max-w-none">
          <p className="text-muted-foreground text-lg">
            Please read these terms of service carefully before using our platform.
          </p>
          <p className="text-muted-foreground mt-4">
            This page is under construction. Check back soon for our complete terms of service.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
