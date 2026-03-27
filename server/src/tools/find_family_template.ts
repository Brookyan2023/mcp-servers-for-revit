import fs from "fs";
import os from "os";
import path from "path";
import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";

const localeFolderHints: Record<string, string[]> = {
  "en": ["English_I", "English", "ENU"],
  "en-us": ["English_I", "English", "ENU"],
  "zh": ["Chinese_I", "Chinese", "CHS"],
  "zh-cn": ["Chinese_I", "Chinese", "CHS"],
};

function normalize(value: string | undefined) {
  return (value ?? "").trim().toLowerCase();
}

function buildRoots(revitVersion?: string, locale?: string) {
  const versions = revitVersion ? [revitVersion] : ["2026", "2025", "2024", "2023", "2022"];
  const localeHints = localeFolderHints[normalize(locale)] ?? [];
  const roots = new Set<string>();

  for (const version of versions) {
    const base = path.join("C:\\ProgramData\\Autodesk", `RVT ${version}`, "Family Templates");
    roots.add(base);
    for (const hint of localeHints) {
      roots.add(path.join(base, hint));
    }
  }

  roots.add(path.join("C:\\ProgramData\\Autodesk\\Revit", "Family Templates"));
  roots.add(path.join(os.homedir(), "Documents", "Autodesk"));
  return Array.from(roots);
}

function walkTemplates(root: string, sink: string[]) {
  if (!fs.existsSync(root)) return;

  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      walkTemplates(fullPath, sink);
      continue;
    }

    if (entry.isFile() && entry.name.toLowerCase().endsWith(".rft")) {
      sink.push(fullPath);
    }
  }
}

function scoreTemplate(fullPath: string, query: string, locale?: string) {
  const haystack = fullPath.toLowerCase();
  const terms = normalize(query).split(/\s+/).filter(Boolean);
  let score = 0;

  for (const term of terms) {
    if (haystack.includes(term)) score += 5;
    if (path.basename(haystack).includes(term)) score += 5;
  }

  if (locale) {
    const hints = localeFolderHints[normalize(locale)] ?? [];
    if (hints.some((hint) => haystack.includes(hint.toLowerCase()))) score += 3;
  }

  if (haystack.includes("detail")) score += 1;
  return score;
}

export function registerFindFamilyTemplateTool(server: McpServer) {
  server.tool(
    "find_family_template",
    "Search common Autodesk family-template folders for .rft files matching a category or keyword query, with optional locale and Revit-version hints.",
    {
      query: z.string().describe("Template search keywords, e.g. detail item or annotation symbol"),
      locale: z.string().optional().describe("Optional locale hint such as en-US or zh-CN"),
      revitVersion: z.string().optional().describe("Optional Revit version such as 2026"),
      maxResults: z.number().optional().default(20).describe("Maximum number of matching templates to return"),
    },
    async ({ query, locale, revitVersion, maxResults }) => {
      try {
        const matches: string[] = [];
        for (const root of buildRoots(revitVersion, locale)) {
          walkTemplates(root, matches);
        }

        const ranked = matches
          .map((fullPath) => ({
            path: fullPath,
            filename: path.basename(fullPath),
            score: scoreTemplate(fullPath, query, locale),
          }))
          .filter((item) => item.score > 0)
          .sort((a, b) => b.score - a.score || a.filename.localeCompare(b.filename))
          .slice(0, maxResults);

        return { content: [{ type: "text", text: JSON.stringify({ count: ranked.length, templates: ranked }, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `find family template failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
