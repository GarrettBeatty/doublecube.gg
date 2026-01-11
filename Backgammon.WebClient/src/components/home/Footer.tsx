import { Card, CardContent } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'

interface NewsItem {
  id: string
  content: React.ReactNode
  timeAgo: string
}

const newsItems: NewsItem[] = [
  {
    id: '1',
    content: (
      <>
        Congratulations to team Saqochess & friends and fan club for winning the $10,000 ChessMood 20/20 Grand Prix Final with top scorers IM Reza Mahdavi, @catask, IM Jakub Pulpan, FM Semyon Puzyrevsky, GM Haik Martirosyan, and many others! Thanks to all participating streamers and 3,135 registered players, including 313 titled players!
      </>
    ),
    timeAgo: '1 week ago',
  }
]

const footerLinks = {
  primary: [
    { label: 'Donate', href: '#' },
    { label: 'Become a Patron', href: '#' },
    { label: 'Swag Store', href: '#' },
  ],
  about: [
    { label: 'About', href: '#' },
    { label: 'FAQ', href: '#' },
    { label: 'Contact', href: '#' },
    { label: 'Mobile App', href: '#' },
  ],
  legal: [
    { label: 'Terms of Service', href: '#' },
    { label: 'Privacy', href: '#' },
    { label: 'Source Code', href: '#' },
    { label: 'Ads', href: '#' },
  ],
  social: [
    { label: 'Mastodon', href: '#' },
    { label: 'GitHub', href: '#' },
    { label: 'Discord', href: '#' },
    { label: 'Bluesky', href: '#' },
    { label: 'YouTube', href: '#' },
    { label: 'Twitch', href: '#' },
  ],
}

export function Footer() {
  return (
    <footer className="bg-muted/50 border-t mt-12">
      <div className="container mx-auto px-4 py-8">
        {/* News Updates Section */}
        <Card className="mb-8">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold">Latest Updates</h3>
              <a href="#" className="text-sm text-primary hover:underline">
                All updates &raquo;
              </a>
            </div>
            <div className="space-y-4">
              {newsItems.map((item, index) => (
                <div key={item.id}>
                  <div className="flex flex-col gap-1">
                    <p className="text-sm text-muted-foreground leading-relaxed">
                      {item.content}
                    </p>
                    <span className="text-xs text-muted-foreground/70">
                      {item.timeAgo}
                    </span>
                  </div>
                  {index < newsItems.length - 1 && <Separator className="mt-4" />}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Links Section */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-8">
          {/* Primary Links */}
          <div>
            <h4 className="font-semibold mb-3">Support</h4>
            <ul className="space-y-2">
              {footerLinks.primary.map((link) => (
                <li key={link.label}>
                  <a
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          {/* About Links */}
          <div>
            <h4 className="font-semibold mb-3">About</h4>
            <ul className="space-y-2">
              {footerLinks.about.map((link) => (
                <li key={link.label}>
                  <a
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          {/* Legal Links */}
          <div>
            <h4 className="font-semibold mb-3">Legal</h4>
            <ul className="space-y-2">
              {footerLinks.legal.map((link) => (
                <li key={link.label}>
                  <a
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          {/* Social Links */}
          <div>
            <h4 className="font-semibold mb-3">Social</h4>
            <ul className="space-y-2">
              {footerLinks.social.map((link) => (
                <li key={link.label}>
                  <a
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* Bottom Bar */}
        <Separator className="mb-4" />
        <div className="flex flex-col md:flex-row justify-between items-center gap-4 text-sm text-muted-foreground">
          <p>Play backgammon for free, forever.</p>
          <p>&copy; {new Date().getFullYear()} Backgammon. All rights reserved.</p>
        </div>
      </div>
    </footer>
  )
}
