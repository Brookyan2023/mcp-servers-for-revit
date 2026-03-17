import { RevitClientConnection } from "./SocketClient.js";
import fs from "fs";
import path from "path";

// Mutex to serialize all Revit connections - prevents race conditions
// when multiple requests are made in parallel
let connectionMutex: Promise<void> = Promise.resolve();

/**
 * 连接到Revit客户端并执行操作
 * @param operation 连接成功后要执行的操作函数
 * @returns 操作的结果
 */
function getRevitPort(): number {
  const rawPort = process.env.REVIT_MCP_PORT;
  const parsedPort = rawPort ? Number(rawPort) : NaN;
  if (Number.isFinite(parsedPort) && parsedPort > 0) {
    return parsedPort;
  }

  const appData = process.env.APPDATA;
  if (appData) {
    const portFilePath = path.join(appData, "RevitMCP", "revit-mcp-port.json");
    if (fs.existsSync(portFilePath)) {
      try {
        const json = JSON.parse(fs.readFileSync(portFilePath, "utf8"));
        const filePort = Number(json?.port);
        if (Number.isFinite(filePort) && filePort > 0) {
          return filePort;
        }
      } catch {
        // Ignore and fall through to error.
      }
    }
  }

  throw new Error(
    "Revit MCP port not available. Start Revit and toggle MCP to generate the port state file, or set REVIT_MCP_PORT."
  );
}

export async function withRevitConnection<T>(
  operation: (client: RevitClientConnection) => Promise<T>
): Promise<T> {
  // Wait for any pending connection to complete before starting a new one
  const previousMutex = connectionMutex;
  let releaseMutex: () => void;
  connectionMutex = new Promise<void>((resolve) => {
    releaseMutex = resolve;
  });
  await previousMutex;

  const port = getRevitPort();
  const revitClient = new RevitClientConnection("localhost", port);

  try {
    // 连接到Revit客户端
    if (!revitClient.isConnected) {
      await new Promise<void>((resolve, reject) => {
        const onConnect = () => {
          revitClient.socket.removeListener("connect", onConnect);
          revitClient.socket.removeListener("error", onError);
          resolve();
        };

        const onError = (error: any) => {
          revitClient.socket.removeListener("connect", onConnect);
          revitClient.socket.removeListener("error", onError);
          reject(new Error("connect to revit client failed"));
        };

        revitClient.socket.on("connect", onConnect);
        revitClient.socket.on("error", onError);

        revitClient.connect();

        setTimeout(() => {
          revitClient.socket.removeListener("connect", onConnect);
          revitClient.socket.removeListener("error", onError);
          reject(new Error("连接到Revit客户端失败"));
        }, 5000);
      });
    }

    // 执行操作
    return await operation(revitClient);
  } finally {
    // 断开连接
    revitClient.disconnect();
    // Release the mutex so the next request can proceed
    releaseMutex!();
  }
}
