export default {
  debug: true,
  plugins: [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    [
      "semantic-release-net",
      {
        sources: [
          {
            url: "https://api.nuget.org/v3/index.json",
            apiKeyEnvVar: "NUGET_API_KEY",
          },
        ],
      },
    ],
    [
      "@semantic-release/github",
      {
        successComment: false,
        failComment: false,
        releasedLabels: false,
        assets: [
          {
            path: "src/**/bin/Release/**/*.nupkg",
          },
        ],
      },
    ],
  ],
};
