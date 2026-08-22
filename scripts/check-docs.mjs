// Fails when a document says something the repository does not support.
//
// It exists because three documents drifted at once and none of them failed anything: a route
// renamed out from under `frontend/CLAUDE.md`, a claim in the root file that stopped being true,
// and a numbered standard contradicting the one above it. See P7 in the harness backlog.
//
// Two checks, and both are narrow on purpose. The first pass of this script found 28 problems of
// which one was real; every false positive taught where a document is allowed to name something
// that does not exist.
//
//   Layout blocks in a CLAUDE.md -- the map, and the only place a path must resolve. Prose in the
//                  same file legitimately names a generated file, an npm specifier, an HTTP route
//                  or a server that is planned and not built. A `## Layout` block does not: it
//                  claims to describe the tree as it is.
//   Citations under docs/ -- `standard N` must resolve to a numbered standard in the root file,
//                  and `ADR-###` / `TD-###` must resolve to a record on disk. Skills and design
//                  docs are excluded because they cite ids as examples while teaching a format.
//
// What it cannot check is the third failure: a standard that is internally consistent, resolvable
// and wrong. That one needs a person, which is why standard 18 exists alongside this file.
//
// Run: node scripts/check-docs.mjs

import { readFileSync, readdirSync, statSync, existsSync } from "node:fs";
import { join, dirname, resolve, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");

const SKIP_DIRS = new Set(["node_modules", ".git", ".next", "bin", "obj", "test-results", ".idea"]);
const SKIP_FILES = new Set(["AGENTS.md"]); // machine-managed by `next dev`; not ours to keep true
const KNOWN_EXTENSIONS =
  /\.(md|mjs|ts|tsx|js|jsx|cs|csproj|slnx|yml|yaml|json|css|py|sh|ps1|txt|woff2|sql|http|lock|toml)$/;

function walk(dir, visit) {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      if (!SKIP_DIRS.has(entry)) {
        visit(full, true);
        walk(full, visit);
      }
    } else {
      visit(full, false);
    }
  }
}

const markdown = [];
walk(ROOT, (path, isDir) => {
  if (!isDir && path.endsWith(".md") && !SKIP_FILES.has(path.split(sep).pop())) markdown.push(path);
});

/**
 * Every path under `base`, in posix form, so a layout entry can be matched by suffix. Layout
 * blocks are written as an indented tree -- `layout.tsx` sits under `app/` and is not a path from
 * the tier root -- so suffix matching is what lets the notation stay readable while still failing
 * when the thing it names is gone.
 */
function indexOf(base) {
  const paths = new Set();
  walk(base, (path) => paths.add(relative(base, path).split(sep).join("/")));
  return paths;
}

const indexes = new Map();
const indexFor = (base) => {
  if (!indexes.has(base)) indexes.set(base, indexOf(base));
  return indexes.get(base);
};

function isPathCandidate(token) {
  if (!/^[A-Za-z0-9._()\-/]+$/.test(token)) return false; // placeholders like <name> or [lang]
  if (token.startsWith("/")) return false; // an HTTP route or a slash command, never a file
  return token.includes("/") || KNOWN_EXTENSIONS.test(token);
}

function resolvesUnder(base, token) {
  const cleaned = token.replace(/\/+$/, "");
  if (existsSync(join(base, cleaned))) return true;
  for (const path of indexFor(base)) {
    if (path === cleaned || path.endsWith(`/${cleaned}`)) return true;
  }
  return false;
}

const failures = [];
const report = (file, line, message) =>
  failures.push(`${relative(ROOT, file).split(sep).join("/")}:${line}  ${message}`);

const rootClaude = readFileSync(join(ROOT, "CLAUDE.md"), "utf8");
const standards = new Set([...rootClaude.matchAll(/^(\d{1,2})\.\s+\*\*/gm)].map((m) => Number(m[1])));

const recordExists = (dir, id) =>
  existsSync(join(ROOT, dir)) &&
  readdirSync(join(ROOT, dir)).some((f) => f.startsWith(`${id}-`) && f.endsWith(".md"));

for (const file of markdown) {
  const lines = readFileSync(file, "utf8").split("\n");
  const isClaudeMd = file.endsWith(`${sep}CLAUDE.md`);
  const isProductDoc = relative(ROOT, file).split(sep)[0] === "docs";
  const base = dirname(file);
  let fenced = false;

  lines.forEach((line, index) => {
    const lineNumber = index + 1;

    if (line.trimStart().startsWith("```")) {
      fenced = !fenced;
      return;
    }

    if (isClaudeMd && fenced) {
      const token = line.trim().split(/\s+/)[0] ?? "";
      if (isPathCandidate(token) && !resolvesUnder(base, token)) {
        report(file, lineNumber, `nothing named ${token} exists here`);
      }
    }

    if (!isProductDoc) return;

    for (const match of line.matchAll(/\bstandards?\b\s+((?:\d+)(?:\s*(?:,|and)\s*\d+)*)/gi)) {
      for (const n of match[1].match(/\d+/g).map(Number)) {
        if (!standards.has(n)) report(file, lineNumber, `standard ${n} does not exist`);
      }
    }
    for (const [, id] of line.matchAll(/\b(ADR-\d{3})\b/g)) {
      if (!recordExists("docs/decisions", id)) report(file, lineNumber, `no record for ${id}`);
    }
    for (const [, id] of line.matchAll(/\b(TD-\d{3})\b/g)) {
      if (!recordExists(".claude/skills/protocol-training/decisions", id)) {
        report(file, lineNumber, `no record for ${id}`);
      }
    }
  });
}

if (failures.length) {
  console.error(`check-docs: ${failures.length} problem(s)\n`);
  for (const failure of failures) console.error(`  ${failure}`);
  console.error("\nA document is corrected by the commit that falsifies it (standard 18).");
  process.exit(1);
}

console.log(`check-docs: ${markdown.length} documents, no drift.`);
