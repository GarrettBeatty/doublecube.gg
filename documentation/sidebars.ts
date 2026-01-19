import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/installation',
        'getting-started/quick-start',
        'getting-started/development',
      ],
    },
    {
      type: 'category',
      label: 'Architecture',
      items: [
        'architecture/overview',
        'architecture/core-engine',
        'architecture/server',
        'architecture/frontend',
      ],
    },
    {
      type: 'category',
      label: 'Features',
      items: [
        'features/multiplayer',
        'features/match-play',
        'features/elo-ratings',
        'features/analysis',
        'features/ai',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      items: [
        'api/overview',
        'api/rest-api',
        'api/signalr-hub',
      ],
    },
    {
      type: 'category',
      label: 'Deployment',
      items: [
        'deployment/local',
        'deployment/aws',
        'deployment/gnubg-setup',
      ],
    },
  ],
};

export default sidebars;
