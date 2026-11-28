#!/usr/bin/env node
import { createHash } from "node:crypto";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const outPath = resolve(repoRoot, process.argv[2] ?? "artifacts/sbom/ruth.cdx.json");

function collectNuGetPackages() {
  const xml = readFileSync(resolve(repoRoot, "Directory.Packages.props"), "utf8");
  const matches = [...xml.matchAll(/<PackageVersion\s+Include="([^"]+)"\s+Version="([^"]+)"\s*\/>/gms)];
  return matches.map(([, name, version]) => ({
    type: "library",
    name,
    version,
    purl: `pkg:nuget/${name}@${version}`,
    scope: "required",
  }));
}

function collectDotnetTools() {
  const manifest = JSON.parse(readFileSync(resolve(repoRoot, "dotnet-tools.json"), "utf8"));
  return Object.entries(manifest.tools ?? {}).map(([name, info]) => ({
    type: "application",
    name,
    version: info.version,
    purl: `pkg:nuget/${name}@${info.version}`,
    scope: "optional",
  }));
}

function collectNpmPackages() {
  const lock = JSON.parse(readFileSync(resolve(repoRoot, "package-lock.json"), "utf8"));
  const seen = new Map();
  for (const [pkgPath, meta] of Object.entries(lock.packages ?? {})) {
    if (!pkgPath || !meta.version) {
      continue;
    }
    const idx = pkgPath.lastIndexOf("node_modules/");
    const name = idx >= 0 ? pkgPath.slice(idx + "node_modules/".length) : pkgPath;
    const encodedName = name.startsWith("@") ? `%40${name.slice(1)}` : name;
    const purl = `pkg:npm/${encodedName}@${meta.version}`;
    if (!seen.has(purl)) {
      seen.set(purl, {
        type: "library",
        name,
        version: meta.version,
        purl,
        scope: meta.dev ? "optional" : "required",
      });
    }
  }
  return [...seen.values()];
}

function collectContainerImages() {
  const sources = [
    readFileSync(resolve(repoRoot, "deploy/docker/dotnet/Dockerfile"), "utf8"),
    readFileSync(resolve(repoRoot, "deploy/docker/postgres/Dockerfile"), "utf8"),
    readFileSync(resolve(repoRoot, "docker-compose.yml"), "utf8"),
  ].join("\n");

  const refs = new Map();
  const pattern = /(?:FROM|image:)\s+([^\s@]+):([^\s@]+)@(sha256:[0-9a-f]{64})/g;
  for (const [, imageName, tag, digest] of sources.matchAll(pattern)) {
    const purl = `pkg:oci/${imageName}@${digest}?tag=${encodeURIComponent(tag)}`;
    refs.set(purl, {
      type: "container",
      name: imageName,
      version: `${tag}@${digest}`,
      purl,
      hashes: [{ alg: "SHA-256", content: digest.slice("sha256:".length) }],
    });
  }
  return [...refs.values()];
}

function deterministicUuidFromHex(sha256Hex) {
  const b = Buffer.from(sha256Hex.slice(0, 32), "hex");
  b[6] = (b[6] & 0x0f) | 0x50;
  b[8] = (b[8] & 0x3f) | 0x80;
  const h = b.toString("hex");
  return `urn:uuid:${h.slice(0, 8)}-${h.slice(8, 12)}-${h.slice(12, 16)}-${h.slice(16, 20)}-${h.slice(20, 32)}`;
}

const components = [
  ...collectNuGetPackages(),
  ...collectDotnetTools(),
  ...collectNpmPackages(),
  ...collectContainerImages(),
]
  .sort((a, b) => a.purl.localeCompare(b.purl))
  .map((comp) => ({
    "bom-ref": comp.purl,
    ...comp,
  }));

const contentDigest = createHash("sha256").update(JSON.stringify(components)).digest("hex");
const sbom = {
  bomFormat: "CycloneDX",
  specVersion: "1.6",
  serialNumber: deterministicUuidFromHex(contentDigest),
  version: 1,
  metadata: {
    component: {
      "bom-ref": "pkg:generic/ruth-platform@1.0.0",
      type: "application",
      name: "ruth-platform",
      version: "1.0.0",
    },
    properties: [
      { name: "ruth:sbom:deterministic-sha256", value: contentDigest },
    ],
  },
  components,
  dependencies: [
    {
      ref: "pkg:generic/ruth-platform@1.0.0",
      dependsOn: components.map((c) => c["bom-ref"]),
    },
  ],
};

mkdirSync(dirname(outPath), { recursive: true });
writeFileSync(outPath, `${JSON.stringify(sbom, null, 2)}\n`, "utf8");
console.log(`Generated CycloneDX 1.6 SBOM (${components.length} components) -> ${outPath}`);
