import fs from "fs";
import os from "os";
import path from "path";

type DeveloperModeState = {
  enabled: boolean;
  expiresAtUtc?: string;
  updatedAtUtc?: string;
  note?: string;
};

const STATE_DIR = path.join(os.homedir(), ".revit-mcp");
const STATE_FILE = path.join(STATE_DIR, "developer-mode.json");
const MAX_CODE_LENGTH = 12000;
const DEFAULT_UNLOCK_MINUTES = 120;
const MAX_UNLOCK_MINUTES = 120;

const BLOCKED_PATTERNS = [
  "System.IO",
  "System.Net",
  "System.Diagnostics",
  "Microsoft.Win32",
  "System.Reflection",
  "DllImport",
  "Process.",
  "File.",
  "Directory.",
  "HttpClient",
  "WebClient",
  "Socket",
  "Registry",
  "Assembly.",
  "AppDomain",
  "Environment.Exit",
];

function readState(): DeveloperModeState | null {
  try {
    if (!fs.existsSync(STATE_FILE)) {
      return null;
    }

    const raw = fs.readFileSync(STATE_FILE, "utf8");
    return JSON.parse(raw) as DeveloperModeState;
  } catch {
    return null;
  }
}

function getAutoUnlockMinutes(): number {
  const raw = process.env.REVIT_MCP_DEVELOPER_MODE_MINUTES;
  if (!raw) {
    return DEFAULT_UNLOCK_MINUTES;
  }

  const minutes = Number.parseInt(raw, 10);
  if (!Number.isFinite(minutes) || minutes <= 0) {
    return DEFAULT_UNLOCK_MINUTES;
  }

  return Math.min(minutes, MAX_UNLOCK_MINUTES);
}

function isAutoRenewEnabled(): boolean {
  return process.env.REVIT_MCP_AUTO_ENABLE_DEVELOPER_MODE === "1";
}

export function refreshDeveloperModeIfEnabled(): void {
  if (process.env.REVIT_MCP_MODE !== "developer") {
    return;
  }

  if (process.env.REVIT_MCP_ENABLE_CODE_EXECUTION !== "1") {
    return;
  }

  if (!isAutoRenewEnabled()) {
    return;
  }

  const now = new Date();
  const minutes = getAutoUnlockMinutes();
  const expiresAt = new Date(now.getTime() + minutes * 60 * 1000);
  const state: DeveloperModeState = {
    enabled: true,
    updatedAtUtc: now.toISOString(),
    expiresAtUtc: expiresAt.toISOString(),
    note: `Auto-renewed by MCP server startup for ${minutes} minutes`,
  };

  fs.mkdirSync(STATE_DIR, { recursive: true });
  fs.writeFileSync(STATE_FILE, JSON.stringify(state, null, 2), "utf8");
  console.error(`Developer mode auto-renewed until ${expiresAt.toISOString()}`);
}

export function getDeveloperModeStatePath(): string {
  return STATE_FILE;
}

export function getDeveloperModeBlockReason(): string | null {
  if (process.env.REVIT_MCP_MODE !== "developer") {
    return "Code execution is only available through the 'revit-mcp-dev' MCP server profile.";
  }

  if (process.env.REVIT_MCP_ENABLE_CODE_EXECUTION !== "1") {
    return "Code execution is disabled by server configuration.";
  }

  refreshDeveloperModeIfEnabled();

  const state = readState();
  if (!state?.enabled) {
    return `Developer mode is locked. Enable it with the local script and try again. State file: ${STATE_FILE}`;
  }

  if (!state.expiresAtUtc) {
    return "Developer mode state is missing an expiration timestamp.";
  }

  const expiresAt = Date.parse(state.expiresAtUtc);
  if (Number.isNaN(expiresAt)) {
    return "Developer mode state has an invalid expiration timestamp.";
  }

  if (Date.now() > expiresAt) {
    return "Developer mode unlock has expired. Re-enable it before running code.";
  }

  return null;
}

export function validateCodeExecutionRequest(code: string): string | null {
  if (code.length > MAX_CODE_LENGTH) {
    return `Code length exceeds the ${MAX_CODE_LENGTH} character safety limit.`;
  }

  const blockedPattern = BLOCKED_PATTERNS.find((pattern) => code.includes(pattern));
  if (blockedPattern) {
    return `Code contains blocked API usage: ${blockedPattern}`;
  }

  return null;
}
