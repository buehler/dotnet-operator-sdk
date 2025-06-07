import { themes as prismThemes } from "prism-react-renderer";

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: "KubeOps - Kubernetes Operators in .NET",
  tagline: "A .NET SDK for building Kubernetes operators",
  favicon: "img/favicon.ico",

  future: {
    v4: true,
  },

  url: "https://buehler.github.io",
  baseUrl: "/dotnet-operator-sdk/",

  organizationName: "buehler",
  projectName: "dotnet-operator-sdk",

  onBrokenLinks: "throw",
  onBrokenMarkdownLinks: "warn",

  i18n: {
    defaultLocale: "en",
    locales: ["en"],
  },

  presets: [
    [
      "classic",
      {
        docs: {
          sidebarPath: "./sidebars.js",
          editUrl: "https://github.com/buehler/dotnet-operator-sdk/tree/main/docs/",
        },
        blog: false,
        theme: {
          customCss: "./src/css/custom.css",
        },
      },
    ],
  ],

  themes: ["@docusaurus/theme-mermaid"],

  themeConfig: {
    image: "img/logo_big.png",
    navbar: {
      title: "KubeOps",
      logo: {
        alt: "dotnet-operator-sdk Logo",
        src: "img/logo.png",
      },
      items: [
        {
          type: "docSidebar",
          position: "left",
          label: "Documentation",
          sidebarId: "operator",
        },
        {
          type: "docSidebar",
          position: "left",
          label: "Packages",
          sidebarId: "packages",
        },
        {
          to: "contribution",
          label: "Contribution",
          position: "left",
        },
        {
          href: "https://github.com/buehler/dotnet-operator-sdk",
          label: "GitHub",
          position: "right",
        },
      ],
    },
    footer: {
      style: "dark",
      links: [
        {
          title: "Docs",
          items: [
            {
              label: "Documentation",
              to: "/docs/operator",
            },
            {
              label: "Packages",
              to: "/docs/packages",
            },
          ],
        },
        {
          title: "Community",
          items: [
            {
              label: "GitHub",
              href: "https://github.com/buehler/dotnet-operator-sdk",
            },
            {
              label: "Issues",
              href: "https://github.com/buehler/dotnet-operator-sdk/issues",
            },
            {
              label: "Discussions",
              href: "https://github.com/buehler/dotnet-operator-sdk/discussions",
            },
            {
              label: "Security",
              href: "https://github.com/buehler/dotnet-operator-sdk/security",
            },
          ],
        },
      ],
      copyright: `${new Date().getFullYear()} - KubeOps Maintainers. Built with Docusaurus. Hosted on GitHub Pages.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ["csharp", "bash"],
    },
  },
};

export default config;
