export type RoomType = "living-room" | "bedroom" | "kitchen";
export type SourceType = "upload" | "canvas";
export type RenderStyle = "modern" | "japandi" | "indochine" | "minimalist";
export type RenderJobStatus = "queued" | "processing" | "succeeded" | "failed";

export interface AuthResponse {
  email: string;
  availableCredits: number;
  reservedCredits: number;
}

export interface ProjectResponse {
  id: string;
  name: string;
  roomType: RoomType;
  createdAt: string;
}

export interface CreateProjectInput {
  name: string;
  roomType: RoomType;
}

export interface UploadImageResponse {
  url: string;
  width: number;
  height: number;
  sourceType: SourceType;
}

export interface CreateRenderJobInput {
  projectId: string;
  sourceUrl: string;
  sourceType: SourceType;
  style: RenderStyle;
  userPrompt: string;
  idempotencyKey: string;
}

export interface CreateRenderJobResponse {
  id: string;
}

export interface RenderJobResponse {
  id: string;
  status: RenderJobStatus;
  progressLabel: string;
  errorCode: string | null;
  resultUrls: string[];
}

interface ApiErrorPayload {
  code?: unknown;
  message?: unknown;
  requestId?: unknown;
}

interface RequestOptions {
  method?: "GET" | "POST" | "DELETE";
  body?: object | FormData;
}

export class ApiError extends Error {
  readonly code: string;
  readonly requestId?: string;
  readonly status: number;

  constructor(code: string, message: string, status: number, requestId?: string) {
    super(message);
    this.name = "ApiError";
    this.code = code;
    this.status = status;
    this.requestId = requestId;
  }
}

function readErrorPayload(value: unknown): ApiErrorPayload {
  return typeof value === "object" && value !== null ? value : {};
}

async function throwApiError(response: Response): Promise<never> {
  const payload = readErrorPayload(await response.json().catch(() => undefined));
  const code = typeof payload.code === "string" && payload.code
    ? payload.code
    : `http_${response.status}`;
  const message = typeof payload.message === "string" && payload.message
    ? payload.message
    : response.statusText || "The API request failed.";
  const requestId = typeof payload.requestId === "string" && payload.requestId
    ? payload.requestId
    : response.headers.get("x-request-id") || undefined;

  throw new ApiError(code, message, response.status, requestId);
}

async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let body: BodyInit | undefined;
  const headers: Record<string, string> = {};
  if (options.body instanceof FormData) {
    body = options.body;
  } else if (options.body !== undefined) {
    body = JSON.stringify(options.body);
    headers["Content-Type"] = "application/json";
  }

  const response = await fetch(path, {
    method: options.method ?? "GET",
    credentials: "include",
    headers,
    body,
  });

  if (!response.ok) await throwApiError(response);
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

const api = {
  register(email: string, password: string) {
    return apiFetch<AuthResponse>("/api/auth/register", {
      method: "POST",
      body: { email, password },
    });
  },

  login(email: string, password: string) {
    return apiFetch<void>("/api/auth/login", {
      method: "POST",
      body: { email, password },
    });
  },

  logout() {
    return apiFetch<void>("/api/auth/logout", { method: "POST" });
  },

  getMe() {
    return apiFetch<AuthResponse>("/api/me");
  },

  listProjects() {
    return apiFetch<ProjectResponse[]>("/api/projects");
  },

  getProject(id: string) {
    return apiFetch<ProjectResponse>(`/api/projects/${encodeURIComponent(id)}`);
  },

  createProject(input: CreateProjectInput) {
    return apiFetch<ProjectResponse>("/api/projects", {
      method: "POST",
      body: input,
    });
  },

  deleteProject(id: string) {
    return apiFetch<void>(`/api/projects/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  uploadImage(file: File, projectId?: string) {
    const body = new FormData();
    body.append("file", file);
    if (projectId !== undefined) body.append("projectId", projectId);
    return apiFetch<UploadImageResponse>("/api/uploads", { method: "POST", body });
  },

  createRenderJob(input: CreateRenderJobInput) {
    return apiFetch<CreateRenderJobResponse>("/api/render-jobs", {
      method: "POST",
      body: input,
    });
  },

  getRenderJob(id: string) {
    return apiFetch<RenderJobResponse>(`/api/render-jobs/${encodeURIComponent(id)}`);
  },
};

export default api;
