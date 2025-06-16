import useDocusaurusContext from "@docusaurus/useDocusaurusContext";
import Layout from "@theme/Layout";
import clsx from "clsx";

import Heading from "@theme/Heading";
import styles from "./index.module.css";

import LogoUrl from "@site/static/img/logo.png";

function HomepageHeader() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <header className={clsx("hero hero--primary", styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div style={{ display: "inline-flex", background: "black", padding: "1rem", borderRadius: "9999px" }}>
          <img src={LogoUrl} alt="KubeOps Logo" className={styles.logo} />
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout title={`${siteConfig.title}`} description="Main page of the documentation">
      <HomepageHeader />
      <main>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", marginTop: "1rem" }}>
          <h2>Join our Discord</h2>
          <iframe
            src="https://discord.com/widget?id=1384101796649242675&theme=dark"
            width="350"
            height="500"
            allowtransparency="true"
            frameborder="0"
            sandbox="allow-popups allow-popups-to-escape-sandbox allow-same-origin allow-scripts"
          ></iframe>
        </div>
      </main>
    </Layout>
  );
}
