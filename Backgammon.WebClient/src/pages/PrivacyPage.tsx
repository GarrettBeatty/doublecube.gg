import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export const PrivacyPage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <Card>
        <CardHeader>
          <CardTitle className="text-3xl">Privacy Policy</CardTitle>
        </CardHeader>
        <CardContent className="prose prose-invert max-w-none">
          <p className="text-muted-foreground text-lg">
            Your privacy is important to us. This policy explains how we collect, use, and protect your information.
          </p>
          <p className="text-muted-foreground mt-4">
            This page is under construction. Check back soon for our complete privacy policy.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
