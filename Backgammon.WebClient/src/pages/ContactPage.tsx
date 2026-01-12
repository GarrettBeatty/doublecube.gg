import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export const ContactPage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <Card>
        <CardHeader>
          <CardTitle className="text-3xl">Contact Us</CardTitle>
        </CardHeader>
        <CardContent className="prose prose-invert max-w-none">
          <p className="text-muted-foreground text-lg">
            Have questions, feedback, or need support? We'd love to hear from you.
          </p>
          <p className="text-muted-foreground mt-4">
            This page is under construction. Check back soon for contact information and a support form.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
