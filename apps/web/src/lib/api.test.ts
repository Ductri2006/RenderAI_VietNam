import { afterEach, describe, expect, it, vi } from "vitest";

afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

async function loadApi() {
  vi.resetModules();
  return import("./api");
}

describe("api client", () => {
  it("uses same-origin paths, credentials, and JSON request bodies", async () => {
    const { default: api } = await loadApi();
    const fetchSpy = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify({ email: "owner@example.com", availableCredits: 20, reservedCredits: 0 }),
        { status: 201, headers: { "Content-Type": "application/json" } },
      ),
    );
    vi.stubGlobal("fetch", fetchSpy);

    await api.register("owner@example.com", "StrongPass123!");

    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/auth/register",
      expect.objectContaining({
        credentials: "include",
        method: "POST",
        headers: expect.objectContaining({ "Content-Type": "application/json" }),
        body: JSON.stringify({ email: "owner@example.com", password: "StrongPass123!" }),
      }),
    );
  });

  it("uses a same-origin path for logout", async () => {
    const { default: api } = await loadApi();
    const fetchSpy = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchSpy);

    await api.logout();

    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/auth/logout",
      expect.objectContaining({ credentials: "include" }),
    );
  });

  it("returns undefined for a successful 204 logout response", async () => {
    const { default: api } = await loadApi();
    const fetchSpy = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchSpy);

    await expect(api.logout()).resolves.toBeUndefined();
  });

  it("turns a non-2xx JSON response into a typed ApiError", async () => {
    const { ApiError, default: api } = await loadApi();
    vi.stubGlobal(
      "fetch",
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(JSON.stringify({ code: "invalid_credentials", message: "Email or password is incorrect.", requestId: "req-123" }), {
          status: 401,
          headers: { "Content-Type": "application/json" },
        }),
      ),
    );

    const error = await api.login("owner@example.com", "bad-password").catch((value: unknown) => value);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({
      code: "invalid_credentials",
      message: "Email or password is incorrect.",
      requestId: "req-123",
      status: 401,
    });
  });

  it("falls back to status text and the request header for an empty non-JSON error", async () => {
    const { ApiError, default: api } = await loadApi();
    vi.stubGlobal(
      "fetch",
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response("upstream unavailable", {
          status: 503,
          statusText: "Service Unavailable",
          headers: { "x-request-id": "req-503" },
        }),
      ),
    );

    const error = await api.getMe().catch((value: unknown) => value);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({
      code: "http_503",
      message: "Service Unavailable",
      requestId: "req-503",
      status: 503,
    });
  });

  it("uploads multipart form data without forcing a JSON content type", async () => {
    const { default: api } = await loadApi();
    const fetchSpy = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(JSON.stringify({ url: "https://cdn.example.test/source.png", width: 1024, height: 768, sourceType: "upload" }), {
        status: 201,
        headers: { "Content-Type": "application/json" },
      }),
    );
    vi.stubGlobal("fetch", fetchSpy);
    const file = new File(["image"], "source.png", { type: "image/png" });

    await api.uploadImage(file, "project-123");

    const [, request] = fetchSpy.mock.calls[0];
    expect(request?.credentials).toBe("include");
    expect(request?.method).toBe("POST");
    expect(request?.headers).not.toHaveProperty("Content-Type");
    expect(request?.body).toBeInstanceOf(FormData);
    expect((request?.body as FormData).get("file")).toBe(file);
    expect((request?.body as FormData).get("projectId")).toBe("project-123");
  });
});
