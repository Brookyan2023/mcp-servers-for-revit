import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";

type CommandEntry = {
  commandName: string;
  description?: string;
};

type CommandCatalog = {
  commands?: CommandEntry[];
};

type ScoredCommand = {
  command: CommandEntry;
  score: number;
  reasons: string[];
};

type GapLogEntry = {
  timestampUtc: string;
  taskDescription: string;
  suggestedPath: string;
  candidateNewTool: string | null;
  topMatches: Array<{
    name: string;
    score: number;
  }>;
};

const ACTION_KEYWORDS: Record<string, string[]> = {
  inspect: ["get", "list", "show", "inspect", "query", "find", "analyze", "check", "report"],
  create: ["create", "add", "make", "generate", "place", "draw", "tag", "dimension"],
  modify: ["set", "update", "edit", "change", "modify", "rename", "move", "rotate", "duplicate"],
  delete: ["delete", "remove", "purge"],
};

const DOMAIN_KEYWORDS: Record<string, string[]> = {
  room: ["room", "rooms", "space", "spaces", "department"],
  wall: ["wall", "walls", "partition", "partitions"],
  door: ["door", "doors"],
  window: ["window", "windows"],
  view: ["view", "views", "sheet", "sheets", "plan", "3d", "section", "elevation"],
  family: ["family", "families", "type", "types", "symbol", "symbols"],
  parameter: ["parameter", "parameters", "property", "properties", "metadata"],
  structure: ["beam", "beams", "structural", "framing", "column", "columns"],
  level: ["level", "levels", "story", "stories", "floor", "floors"],
  category: ["category", "categories"],
  element: ["element", "elements", "object", "objects", "selection", "selected"],
  annotation: ["tag", "tags", "dimension", "dimensions", "text", "note", "notes", "annotation"],
  geometry: ["line", "lines", "curve", "curves", "point", "points", "surface", "boundary", "boundaries"],
  material: ["material", "materials", "quantity", "quantities", "takeoff", "takeoffs"],
  code: ["code", "script", "scripts", "c#", "unsupported"],
};

const PHRASE_WEIGHTS: Array<{
  phrases: string[];
  prefer: string[];
  avoid?: string[];
}> = [
  {
    phrases: ["current view", "active view"],
    prefer: ["get_current_view_info", "get_current_view_elements", "list_views"],
  },
  {
    phrases: ["selected element", "selected elements", "selection"],
    prefer: ["get_selected_elements", "operate_element", "get_element_details"],
  },
  {
    phrases: ["parameter", "parameters"],
    prefer: ["get_element_parameters", "set_element_parameter"],
  },
  {
    phrases: ["wall tag", "tag wall", "tag walls"],
    prefer: ["tag_walls"],
  },
  {
    phrases: ["room tag", "tag room", "tag rooms"],
    prefer: ["tag_rooms"],
  },
  {
    phrases: ["material quantity", "material takeoff", "quantities"],
    prefer: ["get_material_quantities"],
  },
  {
    phrases: ["model statistics", "model complexity"],
    prefer: ["analyze_model_statistics"],
  },
  {
    phrases: ["family type", "available family types"],
    prefer: ["get_available_family_types", "list_wall_types"],
  },
  {
    phrases: ["grid", "grid system"],
    prefer: ["create_grid"],
  },
  {
    phrases: ["framing system", "beam system"],
    prefer: ["create_structural_framing_system"],
  },
];

function getServerRoot(): string {
  const __filename = fileURLToPath(import.meta.url);
  const __dirname = path.dirname(__filename);
  return path.resolve(__dirname, "../..");
}

function getCatalogPath(): string {
  return path.resolve(getServerRoot(), "../command.json");
}

function getGapLogDir(): string {
  return path.resolve(getServerRoot(), "../logs");
}

function getGapLogPath(): string {
  return path.join(getGapLogDir(), "tool-gap-log.jsonl");
}

function getGapBacklogPath(): string {
  return path.join(getGapLogDir(), "tool-gap-backlog.json");
}

function loadCommands(): CommandEntry[] {
  const raw = fs.readFileSync(getCatalogPath(), "utf8");
  const catalog = JSON.parse(raw) as CommandCatalog;
  return catalog.commands ?? [];
}

function tokenize(input: string): string[] {
  return input
    .toLowerCase()
    .replace(/[^a-z0-9#\s]/g, " ")
    .split(/\s+/)
    .filter(Boolean);
}

function toWordSet(tokens: string[]): Set<string> {
  return new Set(tokens);
}

function detectActions(taskText: string, words: Set<string>): string[] {
  const actions = Object.entries(ACTION_KEYWORDS)
    .filter(([, keywords]) => keywords.some((keyword) => taskText.includes(keyword) || words.has(keyword)))
    .map(([action]) => action);
  return actions.length > 0 ? actions : ["inspect"];
}

function detectDomains(taskText: string, words: Set<string>): string[] {
  const domains = Object.entries(DOMAIN_KEYWORDS)
    .filter(([, keywords]) => keywords.some((keyword) => taskText.includes(keyword) || words.has(keyword)))
    .map(([domain]) => domain);
  return domains.length > 0 ? domains : ["element"];
}

function scoreCommand(taskText: string, words: Set<string>, actions: string[], domains: string[], command: CommandEntry): ScoredCommand {
  const haystack = `${command.commandName} ${command.description ?? ""}`.toLowerCase();
  let score = 0;
  const reasons: string[] = [];
  const commandActionHits = actions.filter((action) => haystack.includes(action));
  const commandDomainHits = domains.filter((domain) => haystack.includes(domain));

  for (const action of commandActionHits) {
      score += 7;
      reasons.push(`action:${action}`);
  }

  for (const domain of commandDomainHits) {
      score += 6;
      reasons.push(`domain:${domain}`);
  }

  for (const word of words) {
    if (word.length >= 4 && haystack.includes(word)) {
      score += 2;
    }
  }

  for (const phraseRule of PHRASE_WEIGHTS) {
    if (phraseRule.phrases.some((phrase) => taskText.includes(phrase))) {
      if (phraseRule.prefer.includes(command.commandName)) {
        score += 8;
        reasons.push("phrase-match");
      }
      if (phraseRule.avoid?.includes(command.commandName)) {
        score -= 4;
      }
    }
  }

  if (command.commandName === "send_code_to_revit") {
    score -= 3;
    reasons.push("prefer-tools-first");
  }

  const criticalDomains = domains.filter((domain) =>
    ["door", "window", "room", "wall", "parameter", "level", "material", "family"].includes(domain)
  );

  for (const criticalDomain of criticalDomains) {
    if (!haystack.includes(criticalDomain)) {
      score -= 8;
      reasons.push(`missing-critical-domain:${criticalDomain}`);
    }
  }

  if (actions.includes("create") && taskText.includes("tag") && !haystack.includes("tag")) {
    score -= 6;
    reasons.push("missing-tag-action");
  }

  if (domains.includes("door") && !haystack.includes("door")) {
    score -= 6;
  }

  if (domains.includes("window") && !haystack.includes("window")) {
    score -= 6;
  }

  if (domains.includes("annotation") && command.commandName.startsWith("create_") && !haystack.includes("dimension")) {
    score -= 2;
  }

  if (domains.includes("door") && !haystack.includes("door") && command.commandName === "tag_rooms") {
    score -= 5;
  }

  return { command, score, reasons };
}

function recommendToolName(actions: string[], domains: string[]): string {
  const action = actions.includes("create")
    ? "create"
    : actions.includes("modify")
    ? "update"
    : actions.includes("delete")
    ? "delete"
    : "get";

  const primaryDomain = domains.find((domain) => domain !== "code") ?? "element";
  return `${action}_${primaryDomain}_tool`;
}

function criticalDomainsFromDomains(domains: string[]): string[] {
  return domains.filter((domain) =>
    ["door", "window", "room", "wall", "parameter", "level", "material", "family"].includes(domain)
  );
}

function ensureGapLogDir(): void {
  fs.mkdirSync(getGapLogDir(), { recursive: true });
}

function updateGapLog(entry: GapLogEntry): void {
  ensureGapLogDir();
  fs.appendFileSync(getGapLogPath(), `${JSON.stringify(entry)}\n`, "utf8");

  const backlogPath = getGapBacklogPath();
  const backlog = fs.existsSync(backlogPath)
    ? (JSON.parse(fs.readFileSync(backlogPath, "utf8")) as Record<string, { count: number; lastSeenUtc: string; latestTask: string }>)
    : {};

  const key = entry.candidateNewTool ?? "unclassified_gap";
  const existing = backlog[key];
  backlog[key] = {
    count: (existing?.count ?? 0) + 1,
    lastSeenUtc: entry.timestampUtc,
    latestTask: entry.taskDescription,
  };

  fs.writeFileSync(backlogPath, JSON.stringify(backlog, null, 2), "utf8");
}

export function registerSuggestToolingGapTool(server: McpServer) {
  server.tool(
    "suggest_tooling_gap",
    "Advisory tool for MCP workflow planning. Use this when a task may not fit the current Revit toolset. It suggests the best existing tools first, then recommends whether to use developer code execution or add a new tool. Weak matches are logged into a tool-gap backlog automatically.",
    {
      task_description: z
        .string()
        .describe("Natural-language description of the Revit task you want to complete."),
      prefer_safe_tools: z
        .boolean()
        .optional()
        .describe("Prefer explicit tools over code execution. Defaults to true."),
    },
    async (args) => {
      const commands = loadCommands();
      const taskText = args.task_description.toLowerCase();
      const words = toWordSet(tokenize(args.task_description));
      const actions = detectActions(taskText, words);
      const domains = detectDomains(taskText, words);

      const ranked = commands
        .map((command) => scoreCommand(taskText, words, actions, domains, command))
        .sort((a, b) => b.score - a.score);

      const matches = ranked.filter((item) => item.score > 0).slice(0, 5);
      const strongestScore = matches[0]?.score ?? 0;
      const topMatch = matches[0];
      const missingCriticalCoverage = criticalDomainsFromDomains(domains).some(
        (domain) => topMatch && !topMatch.command.commandName.toLowerCase().includes(domain) && !(topMatch.command.description ?? "").toLowerCase().includes(domain)
      );
      const hasStrongMatch = strongestScore >= 12 && !missingCriticalCoverage;
      const wantsSafeTools = args.prefer_safe_tools ?? true;

      const suggestedPath = hasStrongMatch
        ? "use_existing_tools"
        : wantsSafeTools
        ? "add_new_tool"
        : "use_send_code_to_revit_with_approval";

      const candidateTool = hasStrongMatch ? null : recommendToolName(actions, domains);

      if (!hasStrongMatch) {
        updateGapLog({
          timestampUtc: new Date().toISOString(),
          taskDescription: args.task_description,
          suggestedPath,
          candidateNewTool: candidateTool,
          topMatches: matches.map((item) => ({
            name: item.command.commandName,
            score: item.score,
          })),
        });
      }

      const response = {
        task: args.task_description,
        detectedActions: actions,
        detectedDomains: domains,
        suggestedPath,
        existingTools: matches.map((item) => ({
          name: item.command.commandName,
          description: item.command.description ?? "",
          relevanceScore: item.score,
          reasons: item.reasons,
        })),
        recommendation: hasStrongMatch
          ? `Use the strongest matching tool first: ${matches[0].command.commandName}`
          : wantsSafeTools
          ? `No strong tool match found. Recommend adding a dedicated tool such as '${candidateTool}' instead of jumping straight to code execution.`
          : "No strong tool match found. If the task is urgent, use 'send_code_to_revit' only after developer mode is explicitly unlocked.",
        candidateNewTool: candidateTool
          ? {
              name: candidateTool,
              purpose: `Handle tasks like: ${args.task_description}`,
            }
          : null,
        backlogLog: hasStrongMatch
          ? null
          : {
              logFile: getGapLogPath(),
              backlogFile: getGapBacklogPath(),
            },
      };

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(response, null, 2),
          },
        ],
      };
    }
  );
}
