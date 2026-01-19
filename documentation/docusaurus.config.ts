import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Backgammon Docs',
  tagline: 'Multiplayer backgammon with AI and analysis',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://docs.doublecube.gg',
  baseUrl: '/',

  organizationName: 'garrett',
  projectName: 'backgammon',

  onBrokenLinks: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          routeBasePath: '/',
          editUrl: 'https://github.com/garrett/backgammon/tree/main/documentation/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/backgammon-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Backgammon',
      logo: {
        alt: 'Backgammon Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Documentation',
        },
        {
          to: '/api/overview',
          label: 'API Reference',
          position: 'left',
        },
        {
          href: 'https://github.com/garrett/backgammon',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Getting Started',
              to: '/getting-started/installation',
            },
            {
              label: 'Architecture',
              to: '/architecture/overview',
            },
            {
              label: 'API Reference',
              to: '/api/overview',
            },
          ],
        },
        {
          title: 'Features',
          items: [
            {
              label: 'Multiplayer',
              to: '/features/multiplayer',
            },
            {
              label: 'Match Play',
              to: '/features/match-play',
            },
            {
              label: 'AI Opponents',
              to: '/features/ai',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/garrett/backgammon',
            },
            {
              label: 'Play Now',
              href: 'https://doublecube.gg',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} DoubleCube. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'typescript', 'bash', 'json'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
